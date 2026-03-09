using System.Text;
using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.Services.Answer;
using MIBO.ConversationService.Services.Composer.Text;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Chat.Pipeline.Steps;

public sealed class ComposeTextStep : IChatPipelineStep
{
    private readonly ITextComposer _textComposer;
    private readonly IAnswerService _answerService;
    private readonly ChatOrchestratorOptions _opt;

    public ComposeTextStep(
        ITextComposer textComposer,
        IAnswerService answerService,
        IOptions<ChatOrchestratorOptions> opt
    )
    {
        _textComposer = textComposer;
        _answerService = answerService;
        _opt = opt.Value;
    }

    public string Name => "compose_text";

    public async Task ExecuteAsync(ChatPipelineContext context, CancellationToken ct)
    {
        if (context.StreamMode) return;

        if (context.SkipPlanning && !string.IsNullOrWhiteSpace(context.Text))
            return;

        var hasSteps = context.Plan.Steps is { Count: > 0 };
        var hasUi = context.UiV1 is not null;

        // LLM answer when planner produced no tools.
        if (!hasSteps)
        {
            context.Text = hasUi
                ? "Am actualizat interfața interactivă. Poți continua direct din componentele afișate."
                : await _answerService.AnswerAsync(
                    context.Request.ConversationId,
                    context.Request.UserId,
                    context.Request.Prompt,
                    context.ConversationContext,
                    ct
                );
            return;
        }

        // If tool data exists, ask answer model to summarize grounded data.
        if (context.ToolResults.Count > 0)
        {
            var summary = BuildToolSummary(context.ToolResults, _opt.MaxToolResultCharsForText);
            var enrichedPrompt = $"""
                User asked: "{context.Request.Prompt}"

                Retrieved tool data:
                {summary}

                Provide a concise and grounded response.
                If a UI is shown, keep the response to 1-2 short sentences.
                Do not invent values not present in tool data.
                """;

            context.Text = await _answerService.AnswerAsync(
                context.Request.ConversationId,
                context.Request.UserId,
                enrichedPrompt,
                context.ConversationContext,
                ct
            );
            return;
        }

        var deterministic = await _textComposer.ComposeTextAsync(
            context.Request.ConversationId,
            context.Request.UserId,
            context.Request.Prompt,
            context.ToolResults,
            ct
        );

        if (LooksLikeAllMissing(deterministic))
        {
            context.Text = await _answerService.AnswerAsync(
                context.Request.ConversationId,
                context.Request.UserId,
                context.Request.Prompt,
                context.ConversationContext,
                ct
            );
            return;
        }

        context.Text = deterministic;
    }

    private static string BuildToolSummary(
        IReadOnlyDictionary<string, System.Text.Json.JsonElement> toolResults,
        int maxCharsPerTool
    )
    {
        var sb = new StringBuilder();

        foreach (var (tool, body) in toolResults)
        {
            var json = body.ToString();
            if (json.Length > maxCharsPerTool)
                json = json[..maxCharsPerTool] + "... (truncated)";

            sb.AppendLine($"[{tool}] {json}");
        }

        return sb.ToString();
    }

    private static bool LooksLikeAllMissing(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;

        var t = text.Trim();
        if (t.Equals("OK", StringComparison.OrdinalIgnoreCase)) return true;

        if (t.Contains("N/A", StringComparison.OrdinalIgnoreCase))
        {
            var naCount = CountOccurrences(t, "N/A");
            if (naCount >= 2 && t.Length < 240) return true;
        }

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
