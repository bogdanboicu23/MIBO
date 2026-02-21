using System.Globalization;
using System.Text;
using System.Text.Json;
using MIBO.ConversationService.Helper;
using MIBO.ConversationService.Services.Composer.TextSpecProvider;

namespace MIBO.ConversationService.Services.Composer.Text;

public sealed class TextComposer : ITextComposer
{
    private readonly ITextComposeSpecProvider _specProvider;

    public TextComposer(ITextComposeSpecProvider specProvider) => _specProvider = specProvider;

    public async Task<string> ComposeTextAsync(
        string conversationId,
        string userId,
        string userPrompt,
        IReadOnlyDictionary<string, JsonElement> toolResults,
        CancellationToken ct
    )
    {
        var spec = await _specProvider.GetSpecAsync(conversationId, userId, ct);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var b in spec.Bindings)
        {
            if (!toolResults.TryGetValue(b.ToolRef, out var root))
            {
                values[b.Key] = spec.MissingValue;
                continue;
            }

            if (!JsonPathLite.TryGet(root, b.JsonPath, out var el))
            {
                values[b.Key] = spec.MissingValue;
                continue;
            }

            values[b.Key] = Format(el, b.Format, spec.MissingValue);
        }

        // 1) Compose from template if meaningful
        var template = (spec.Template ?? "").Trim();
        var text = string.Empty;

        if (!string.IsNullOrWhiteSpace(template) && !template.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            text = ApplyTemplate(template, values).Trim();
        }

        // 2) If template missing/empty => fallback (deterministic, no LLM)
        if (string.IsNullOrWhiteSpace(text))
        {
            // If you provided fallbackTemplate in config, use it first
            var ft = (spec.FallbackTemplate ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(ft))
                text = ApplyTemplate(ft, values).Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = DeterministicFallback(userPrompt, toolResults);
        }

        return text;
    }

    private static string ApplyTemplate(string template, Dictionary<string, string> values)
    {
        var result = template;
        foreach (var (k, v) in values)
            result = result.Replace("{{" + k + "}}", v, StringComparison.OrdinalIgnoreCase);
        return result;
    }

    private static string Format(JsonElement el, string? format, string missing)
    {
        if (el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return missing;

        if (string.IsNullOrWhiteSpace(format))
            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString() ?? missing,
                _ => el.ToString() ?? missing
            };

        if (format.Equals("currency", StringComparison.OrdinalIgnoreCase))
        {
            if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var d))
                return d.ToString("0.##", CultureInfo.InvariantCulture);
            return el.ToString() ?? missing;
        }

        if (format.Equals("int", StringComparison.OrdinalIgnoreCase))
        {
            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var i))
                return i.ToString(CultureInfo.InvariantCulture);
            return el.ToString() ?? missing;
        }

        if (format.Equals("percent", StringComparison.OrdinalIgnoreCase))
        {
            if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var d))
                return (d * 100m).ToString("0.##", CultureInfo.InvariantCulture) + "%";
            return el.ToString() ?? missing;
        }

        return el.ToString() ?? missing;
    }

    // Deterministic, decoupled fallback:
    // - doesn’t assume specific domains; it tries known keys but degrades gracefully
    private static string DeterministicFallback(string userPrompt, IReadOnlyDictionary<string, JsonElement> toolResults)
    {
        var sb = new StringBuilder();

        // Generic summary of tool results present
        sb.AppendLine($"Am procesat cererea: \"{userPrompt}\".");

        if (toolResults.Count == 0)
        {
            sb.AppendLine("Nu am primit date de la servicii pentru a compune un răspuns bazat pe tool-uri.");
            return sb.ToString().Trim();
        }

        sb.AppendLine("Date primite de la servicii:");

        foreach (var (tool, root) in toolResults)
        {
            // Try to extract common scalar fields if any (best-effort)
            var summary = SummarizeJson(root);
            sb.AppendLine($"- {tool}: {summary}");
        }

        return sb.ToString().Trim();
    }

    private static string SummarizeJson(JsonElement root)
    {
        try
        {
            return root.ValueKind switch
            {
                JsonValueKind.Object => $"object ({root.EnumerateObject().Take(6).Count()} fields...)",
                JsonValueKind.Array => $"array ({root.GetArrayLength()} items)",
                JsonValueKind.String => root.GetString() ?? "string",
                JsonValueKind.Number => root.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => "null"
            };
        }
        catch
        {
            return "unavailable";
        }
    }
}