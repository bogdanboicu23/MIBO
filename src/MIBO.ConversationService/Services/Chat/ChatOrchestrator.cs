using MIBO.ConversationService.DTOs.Contracts;
using MIBO.ConversationService.Services.Chat.Pipeline;

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
    private readonly ILogger<ChatOrchestrator> _logger;

    public ChatOrchestrator(
        IChatPipeline pipeline,
        ILogger<ChatOrchestrator> logger
    )
    {
        _pipeline = pipeline;
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

    public async IAsyncEnumerable<SseEvent> HandleStreamAsync(
        ChatRequest req,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var correlationId = Guid.NewGuid().ToString("N");

        // 1) Persist user message (durable)
        await _store.AppendUserMessageAsync(
            req.ConversationId, req.UserId, req.Prompt, correlationId, ct);

        // 2) Load conversation context
        var conversationContext = await _store.GetPlannerConversationContextAsync(
            req.ConversationId, req.UserId, ct);

        // 3) Build planner input
        var plannerInput = await _plannerInputFactory.BuildAsync(
            req.ConversationId, req.UserId, req.Prompt, conversationContext, ct);

        // 4) Ask planner -> validate -> fallback
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

        // 5) Execute tool plan
        var toolResults = await _planExecutor.ExecuteAsync(plan, ct);

        // 6) Compose UI and emit immediately so the client renders it while text streams
        object? uiV1 = null;
        if (plan.UiIntent is not null)
        {
            uiV1 = await _uiComposer.ComposeUiV1Async(
                req.ConversationId, req.UserId, plan.UiIntent, toolResults, ct);

            yield return new SseEvent("ui", JsonSerializer.Serialize(uiV1));
        }

        // 7) Stream text tokens
        var textBuilder = new System.Text.StringBuilder();
        var prompt = BuildTextPrompt(req.Prompt, conversationContext, plan, toolResults, uiV1);

        await foreach (var token in _answerService.StreamAnswerAsync(
            req.ConversationId, req.UserId, prompt, conversationContext, ct))
        {
            textBuilder.Append(token);
            yield return new SseEvent(null, JsonSerializer.Serialize(new { t = token }));
        }

        var fullText = textBuilder.ToString();

        // 8) Persist assistant message
        await _store.AppendAssistantMessageAsync(
            req.ConversationId, req.UserId, fullText, uiV1, correlationId, ct);

        // 9) Signal done
        yield return new SseEvent("done", JsonSerializer.Serialize(new { correlationId }));
    }

    /// <summary>
    /// Builds the prompt for text composition based on the plan and tool results.
    /// Shared logic between HandleAsync (non-streaming) and HandleStreamAsync (streaming).
    /// </summary>
    private string BuildTextPrompt(
        string userPrompt,
        object conversationContext,
        ToolPlanV1 plan,
        IReadOnlyDictionary<string, JsonElement> toolResults,
        object? uiV1)
    {
        var hasSteps = plan.Steps is { Count: > 0 };
        var hasUi = uiV1 is not null;

        // Case A: general question (no tools, no UI) -> direct user prompt
        if (!hasSteps && !hasUi)
            return userPrompt;

        // Case B: tools returned data -> enriched prompt for contextual summary
        if (toolResults.Count > 0)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("The following data was retrieved from tools:");
            foreach (var (tool, body) in toolResults)
            {
                var json = body.ToString();
                if (json.Length > 2000)
                    json = json[..2000] + "... (truncated)";
                sb.AppendLine($"[{tool}]: {json}");
            }

            return $"""
                User asked: "{userPrompt}"

                {sb}

                Based on the data above, provide a brief, friendly summary answering the user's question.
                If a UI is being shown alongside this text, keep your answer short (1-2 sentences) and refer the user to the displayed data.
                Do NOT invent data — use only what was provided above.
                """;
        }

        // Case C: tools ran but no results -> direct user prompt (will be a short answer)
        return userPrompt;
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