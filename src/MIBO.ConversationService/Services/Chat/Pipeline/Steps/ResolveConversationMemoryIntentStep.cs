using System.Text.Json;

namespace MIBO.ConversationService.Services.Chat.Pipeline.Steps;

public sealed class ResolveConversationMemoryIntentStep : IChatPipelineStep
{
    public string Name => "resolve_conversation_memory_intent";

    public Task ExecuteAsync(ChatPipelineContext context, CancellationToken ct)
    {
        if (context.SkipPlanning) return Task.CompletedTask;

        var prompt = (context.Request.Prompt ?? "").Trim();
        if (string.IsNullOrWhiteSpace(prompt)) return Task.CompletedTask;

        if (IsPreviousQuestionIntent(prompt))
        {
            var history = ExtractMessages(context.ConversationContext);
            var previousUserQuestion = FindPreviousUserQuestion(history, prompt);

            context.Text = previousUserQuestion is null
                ? "Nu există o întrebare anterioară înregistrată."
                : $"Întrebarea anterioară a fost: \"{previousUserQuestion}\".";

            context.UiV1 = null;
            context.SkipPlanning = true;
            context.Warnings.Add("conversation_memory_shortcut");
            return Task.CompletedTask;
        }

        if (IsConversationSummaryIntent(prompt))
        {
            var history = ExtractMessages(context.ConversationContext);
            var summary = BuildSummary(history);
            context.Text = string.IsNullOrWhiteSpace(summary)
                ? "Nu am suficiente mesaje în context pentru un rezumat util."
                : summary;
            context.UiV1 = null;
            context.SkipPlanning = true;
            context.Warnings.Add("conversation_summary_shortcut");
        }

        return Task.CompletedTask;
    }

    private static bool IsPreviousQuestionIntent(string prompt)
    {
        var p = prompt.ToLowerInvariant();
        return p.Contains("care a fost intrebarea anterior") ||
               p.Contains("care a fost întrebarea anterior") ||
               p.Contains("intrebarea anterioara") ||
               p.Contains("întrebarea anterioară") ||
               p.Contains("intrebarea precedenta") ||
               p.Contains("întrebarea precedentă") ||
               p.Contains("ce am intrebat") ||
               p.Contains("ce am întrebat") ||
               p.Contains("previous question") ||
               p.Contains("last question") ||
               p.Contains("what was my previous question");
    }

    private static bool IsConversationSummaryIntent(string prompt)
    {
        var p = prompt.ToLowerInvariant();
        return p.Contains("rezuma conversatia") ||
               p.Contains("rezumă conversația") ||
               p.Contains("rezumat conversatie") ||
               p.Contains("rezumat conversație") ||
               p.Contains("summary of the conversation") ||
               p.Contains("summarize conversation");
    }

    private static string? FindPreviousUserQuestion(
        IReadOnlyList<(string Role, string Text)> history,
        string currentPrompt
    )
    {
        var normalizedCurrent = NormalizeText(currentPrompt);

        for (var i = history.Count - 1; i >= 0; i--)
        {
            var (role, text) = history[i];
            if (!string.Equals(role, "user", StringComparison.OrdinalIgnoreCase)) continue;

            var candidate = NormalizeText(text);
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            if (string.Equals(candidate, normalizedCurrent, StringComparison.OrdinalIgnoreCase)) continue;
            return text.Trim();
        }

        return null;
    }

    private static string BuildSummary(IReadOnlyList<(string Role, string Text)> history)
    {
        if (history.Count == 0) return "";

        var recent = history
            .TakeLast(10)
            .Select(m => $"{m.Role}: {TrimText(m.Text, 160)}")
            .ToArray();

        return "Rezumat conversație (ultimele mesaje):\n" + string.Join("\n", recent);
    }

    private static IReadOnlyList<(string Role, string Text)> ExtractMessages(
        IReadOnlyDictionary<string, object?> context
    )
    {
        if (!context.TryGetValue("lastMessages", out var raw) || raw is null)
            return Array.Empty<(string Role, string Text)>();

        if (raw is IEnumerable<object?> enumerable)
            return enumerable
                .Select(ParseMessage)
                .Where(x => x is not null)
                .Select(x => x!.Value)
                .ToArray();

        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            var list = new List<(string Role, string Text)>();
            foreach (var item in je.EnumerateArray())
            {
                var parsed = ParseMessage(item);
                if (parsed is null) continue;
                list.Add(parsed.Value);
            }
            return list;
        }

        return Array.Empty<(string Role, string Text)>();
    }

    private static (string Role, string Text)? ParseMessage(object? raw)
    {
        if (raw is null) return null;

        if (raw is JsonElement je)
        {
            if (je.ValueKind != JsonValueKind.Object) return null;
            var role = TryGetProperty(je, "role");
            var text = TryGetProperty(je, "text");
            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(text)) return null;
            return (role, text);
        }

        if (raw is IReadOnlyDictionary<string, object?> ro)
        {
            ro.TryGetValue("role", out var roleRaw);
            ro.TryGetValue("text", out var textRaw);
            var role = Convert.ToString(roleRaw) ?? "";
            var text = Convert.ToString(textRaw) ?? "";
            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(text)) return null;
            return (role, text);
        }

        if (raw is Dictionary<string, object?> dict)
        {
            dict.TryGetValue("role", out var roleRaw);
            dict.TryGetValue("text", out var textRaw);
            var role = Convert.ToString(roleRaw) ?? "";
            var text = Convert.ToString(textRaw) ?? "";
            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(text)) return null;
            return (role, text);
        }

        return null;
    }

    private static string TryGetProperty(JsonElement obj, string prop)
    {
        foreach (var p in obj.EnumerateObject())
        {
            if (!string.Equals(p.Name, prop, StringComparison.OrdinalIgnoreCase)) continue;
            return p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() ?? "" : p.Value.ToString();
        }
        return "";
    }

    private static string NormalizeText(string value)
    {
        return value.Trim().Replace("\r", "", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    }

    private static string TrimText(string text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var t = text.Trim();
        return t.Length <= maxChars ? t : t[..maxChars] + "...";
    }
}
