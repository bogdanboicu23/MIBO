using MIBO.ConversationService.DTOs.Contracts;
using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.DTOs.PlannerContracts;
using MIBO.ConversationService.Services.Answer;
using MIBO.ConversationService.Services.Composer.Text;
using MIBO.ConversationService.Services.Composer.Ui;
using MIBO.ConversationService.Services.Planner.Client;
using MIBO.ConversationService.Services.Planner.Factory;
using MIBO.ConversationService.Services.Planner.Fallback;
using MIBO.ConversationService.Services.Planner.Validator;
using MIBO.ConversationService.Services.Tools.PlanExecutor;
using MIBO.Storage.Mongo.Store.Conversation;
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
    private readonly IConversationStore _store;
    private readonly IPlannerInputFactory _plannerInputFactory;
    private readonly IPlannerClient _plannerClient;
    private readonly IPlanValidator _planValidator;
    private readonly IToolPlanExecutor _planExecutor;
    private readonly IUiComposer _uiComposer;
    private readonly ITextComposer _textComposer;
    private readonly IFallbackPlanner _fallbackPlanner;
    private readonly IAnswerService _answerService;
    private readonly ChatOrchestratorOptions _opt;

    public ChatOrchestrator(
        IConversationStore store,
        IPlannerInputFactory plannerInputFactory,
        IPlannerClient plannerClient,
        IPlanValidator planValidator,
        IToolPlanExecutor planExecutor,
        IUiComposer uiComposer,
        ITextComposer textComposer,
        IFallbackPlanner fallbackPlanner,
        IAnswerService answerService,
        IOptions<ChatOrchestratorOptions> opt
    )
    {
        _store = store;
        _plannerInputFactory = plannerInputFactory;
        _plannerClient = plannerClient;
        _planValidator = planValidator;
        _planExecutor = planExecutor;
        _uiComposer = uiComposer;
        _textComposer = textComposer;
        _fallbackPlanner = fallbackPlanner;
        _answerService = answerService;
        _opt = opt.Value;
    }

    public async Task<ChatResponse> HandleAsync(ChatRequest req, CancellationToken ct)
    {
        var correlationId = Guid.NewGuid().ToString("N");

        // 1) Persist user message first (durable)
        await _store.AppendUserMessageAsync(
            req.ConversationId,
            req.UserId,
            req.Prompt,
            correlationId,
            ct
        );

        // 2) Load conversation context (store decides what context contains)
        var conversationContext = await _store.GetPlannerConversationContextAsync(
            req.ConversationId,
            req.UserId,
            ct
        );

        // 3) Build planner input (includes toolCatalog + uiCatalog + constraints)
        var plannerInput = await _plannerInputFactory.BuildAsync(
            req.ConversationId,
            req.UserId,
            req.Prompt,
            conversationContext,
            ct
        );

        // 4) Ask planner -> validate -> fallback (if enabled)
        ToolPlanV1 plan;
        try
        {
            plan = await _plannerClient.PlanAsync(plannerInput, ct);
            _planValidator.ValidateOrThrow(plan);
        }
        catch when (_opt.EnableFallback)
        {
            plan = _fallbackPlanner.CreateFallbackPlan(req.Prompt);
            _planValidator.ValidateOrThrow(plan);
        }

        // 5) Execute tool plan (NO LLM). If steps=0 => toolResults empty.
        var toolResults = await _planExecutor.ExecuteAsync(plan, ct);

        // 6) Compose response
        object? uiV1 = null;
        if (plan.UiIntent is not null)
        {
            uiV1 = await _uiComposer.ComposeUiV1Async(
                req.ConversationId,
                req.UserId,
                plan.UiIntent,
                toolResults,
                ct
            );
        }

        // 6b) Text decision:
        // - If planner chose no tools and no UI => general Q => answer via LLM
        // - Else deterministic from tools; if useless => LLM fallback
        var text = await ComposeTextAsync(
            req.ConversationId,
            req.UserId,
            req.Prompt,
            conversationContext,
            plan,
            toolResults,
            uiV1,
            ct
        );

        // 7) Persist assistant message
        await _store.AppendAssistantMessageAsync(
            req.ConversationId,
            req.UserId,
            text,
            uiV1,
            correlationId,
            ct
        );

        return new ChatResponse(text, uiV1, correlationId);
    }

    private async Task<string> ComposeTextAsync(
        string conversationId,
        string userId,
        string userPrompt,
        object conversationContext,
        ToolPlanV1 plan,
        IReadOnlyDictionary<string, System.Text.Json.JsonElement> toolResults,
        object? uiV1,
        CancellationToken ct
    )
    {
        var hasSteps = plan.Steps is { Count: > 0 };
        var hasUi = uiV1 is not null;

        // Case A: general question (no tools, no UI) -> LLM answer
        if (!hasSteps && !hasUi)
        {
            return await _answerService.AnswerAsync(
                conversationId,
                userId,
                userPrompt,
                conversationContext,
                ct
            );
        }

        // Case B: tools returned data — let LLM compose a contextual summary
        if (toolResults.Count > 0)
        {
            var toolDataSummary = new System.Text.StringBuilder();
            toolDataSummary.AppendLine("The following data was retrieved from tools:");
            foreach (var (tool, body) in toolResults)
            {
                var json = body.ToString();
                // Truncate large payloads to avoid exceeding context
                if (json.Length > 2000)
                    json = json[..2000] + "... (truncated)";
                toolDataSummary.AppendLine($"[{tool}]: {json}");
            }

            var enrichedPrompt = $"""
                User asked: "{userPrompt}"

                {toolDataSummary}

                Based on the data above, provide a brief, friendly summary answering the user's question.
                If a UI is being shown alongside this text, keep your answer short (1-2 sentences) and refer the user to the displayed data.
                Do NOT invent data — use only what was provided above.
                """;

            return await _answerService.AnswerAsync(
                conversationId,
                userId,
                enrichedPrompt,
                conversationContext,
                ct
            );
        }

        // Case C: tools ran but no results — deterministic fallback
        var deterministic = await _textComposer.ComposeTextAsync(
            conversationId,
            userId,
            userPrompt,
            toolResults,
            ct
        );

        if (LooksLikeAllMissing(deterministic))
        {
            return await _answerService.AnswerAsync(
                conversationId,
                userId,
                userPrompt,
                conversationContext,
                ct
            );
        }

        return deterministic;
    }

    // Simple, decoupled heuristic. You can tune this via options later.
    private static bool LooksLikeAllMissing(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;

        var t = text.Trim();

        // If content is largely "N/A", it's not useful.
        // (We avoid coupling to specific tools/domains.)
        if (t.Contains("N/A", StringComparison.OrdinalIgnoreCase))
        {
            var naCount = CountOccurrences(t, "N/A");
            // If multiple N/A occurrences and the message is short => likely useless template output
            if (naCount >= 2 && t.Length < 240) return true;
        }

        // Old default template case
        if (t.Equals("OK", StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    private static int CountOccurrences(string s, string needle)
    {
        var count = 0;
        var idx = 0;
        while ((idx = s.IndexOf(needle, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}