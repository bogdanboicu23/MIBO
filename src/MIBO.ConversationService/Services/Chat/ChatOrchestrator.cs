using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using MIBO.ConversationService.DTOs.Contracts;
using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.Services.Answer;
using MIBO.ConversationService.Services.Chat.Pipeline;
using MIBO.Storage.Mongo.Store.Conversation;
using MIBO.Storage.Mongo.Store.Ui;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Chat;

/// <summary>
/// Orchestrator: message -> planner -> validate -> execute tools -> compose (UI + text)
/// Rules:
/// - Planner called ONLY for new user message
/// - Actions do NOT call planner (handled elsewhere)
/// - If plan has no steps and no UI => answer via LLM text-only (generic, decoupled)
/// - If deterministic text is empty/useless (N/A) => fallback to LLM text-only
/// - UI is composed only when uiIntent != null
/// </summary>
public sealed class ChatOrchestrator : IChatOrchestrator
{
    private readonly IChatPipeline _pipeline;
    private readonly IAnswerService _answerService;
    private readonly IConversationStore _store;
    private readonly IUiInstanceStore _uiInstanceStore;
    private readonly ChatOrchestratorOptions _opt;
    private readonly ILogger<ChatOrchestrator> _logger;

    public ChatOrchestrator(
        IChatPipeline pipeline,
        IAnswerService answerService,
        IConversationStore store,
        IUiInstanceStore uiInstanceStore,
        IOptions<ChatOrchestratorOptions> opt,
        ILogger<ChatOrchestrator> logger
    )
    {
        _pipeline = pipeline;
        _answerService = answerService;
        _store = store;
        _uiInstanceStore = uiInstanceStore;
        _opt = opt.Value;
        _logger = logger;
    }

    public async Task<ChatResponse> HandleAsync(ChatRequest req, CancellationToken ct)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["ConversationId"] = req.ConversationId,
            ["UserId"] = req.UserId
        });

        var context = new ChatPipelineContext(req, correlationId);
        await _pipeline.ExecuteAsync(context, ct);

        return new ChatResponse(
            Text: context.Text,
            UiV1: context.UiV1,
            CorrelationId: correlationId,
            Schema: "chat.response.v2",
            Warnings: context.Warnings
        );
    }

    public async IAsyncEnumerable<SseEvent> HandleStreamAsync(
        ChatRequest req,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["ConversationId"] = req.ConversationId,
            ["UserId"] = req.UserId
        });

        // Run pipeline steps 1-7 (ComposeText and PersistAssistant skip in StreamMode)
        var context = new ChatPipelineContext(req, correlationId) { StreamMode = true };
        await _pipeline.ExecuteAsync(context, ct);

        // If memory intent already resolved text (SkipPlanning), emit it as a single chunk
        if (context.SkipPlanning && !string.IsNullOrWhiteSpace(context.Text))
        {
            if (context.UiV1 is not null)
                yield return new SseEvent("ui", JsonSerializer.Serialize(context.UiV1));

            yield return new SseEvent(null, JsonSerializer.Serialize(new { t = context.Text }));

            await PersistAssistantAsync(req, context.Text, context.UiV1, correlationId, ct);
            yield return new SseEvent("done", JsonSerializer.Serialize(new { correlationId }));
            yield break;
        }

        // Emit UI immediately so the client renders it while text streams
        if (context.UiV1 is not null)
            yield return new SseEvent("ui", JsonSerializer.Serialize(context.UiV1));

        // Build the text prompt (same logic as ComposeTextStep)
        var prompt = BuildStreamPrompt(context);

        // Stream text tokens
        var textBuilder = new StringBuilder();
        await foreach (var token in _answerService.StreamAnswerAsync(
            req.ConversationId, req.UserId, prompt, context.ConversationContext, ct))
        {
            textBuilder.Append(token);
            yield return new SseEvent(null, JsonSerializer.Serialize(new { t = token }));
        }

        var fullText = textBuilder.ToString();

        // Persist assistant message
        await PersistAssistantAsync(req, fullText, context.UiV1, correlationId, ct);

        // Signal done
        yield return new SseEvent("done", JsonSerializer.Serialize(new { correlationId }));
    }

    private async Task PersistAssistantAsync(
        ChatRequest req, string text, object? uiV1, string correlationId, CancellationToken ct)
    {
        await _store.AppendAssistantMessageAsync(
            req.ConversationId, req.UserId, text, uiV1, correlationId, ct);

        if (uiV1 is not null)
        {
            await _uiInstanceStore.UpsertFromAssistantMessageAsync(
                req.ConversationId, req.UserId, uiV1, ct);
        }
    }

    private string BuildStreamPrompt(ChatPipelineContext context)
    {
        var hasSteps = context.Plan.Steps is { Count: > 0 };
        var hasUi = context.UiV1 is not null;

        // No tools planned -> direct user prompt to LLM
        if (!hasSteps)
        {
            if (hasUi)
                return "Am actualizat interfața interactivă. Poți continua direct din componentele afișate.";
            return context.Request.Prompt;
        }

        // Tools returned data -> enriched prompt for contextual summary
        if (context.ToolResults.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Retrieved tool data:");
            foreach (var (tool, body) in context.ToolResults)
            {
                var json = body.ToString();
                if (json.Length > _opt.MaxToolResultCharsForText)
                    json = json[.._opt.MaxToolResultCharsForText] + "... (truncated)";
                sb.AppendLine($"[{tool}] {json}");
            }

            return $"""
                User asked: "{context.Request.Prompt}"

                {sb}

                Provide a concise and grounded response.
                If a UI is shown, keep the response to 1-2 short sentences.
                Do not invent values not present in tool data.
                """;
        }

        // Tools ran but no results
        return context.Request.Prompt;
    }
}