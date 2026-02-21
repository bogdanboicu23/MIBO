using System.Text.Json.Nodes;

namespace MIBO.ConversationService.Chat;

public static class ArgTemplateResolver
{
    public static JsonObject Resolve(JsonObject args, IReadOnlyDictionary<string, JsonNode?> stepOutputs)
    {
        var clone = (JsonObject)args.DeepClone();

        foreach (var kv in clone.ToList())
        {
            if (kv.Value is JsonValue jv && jv.TryGetValue<string>(out var s))
            {
                var resolved = ResolveString(s, stepOutputs);
                clone[kv.Key] = resolved is null ? kv.Value : JsonValue.Create(resolved);
            }
        }

        return clone;
    }

    private static string? ResolveString(string s, IReadOnlyDictionary<string, JsonNode?> stepOutputs)
    {
        // format: {{step:budget.availableAmount}}
        if (!s.StartsWith("{{step:", StringComparison.Ordinal) || !s.EndsWith("}}", StringComparison.Ordinal))
            return null;

        var inner = s.Substring("{{step:".Length, s.Length - "{{step:".Length - 2);
        var dot = inner.IndexOf('.', StringComparison.Ordinal);
        if (dot <= 0) return null;

        var stepId = inner[..dot];
        var path = inner[(dot + 1)..];

        if (!stepOutputs.TryGetValue(stepId, out var node) || node is null)
            return null;

        var cur = node;
        foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (cur is JsonObject obj && obj.TryGetPropertyValue(part, out var next))
                cur = next;
            else
                return null;
        }

        return cur?.ToString();
    }
}