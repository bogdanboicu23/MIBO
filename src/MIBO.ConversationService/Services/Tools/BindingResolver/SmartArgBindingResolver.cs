using System.Text.Json;
using System.Text.RegularExpressions;

namespace MIBO.ConversationService.Services.Tools.BindingResolver;

public sealed class SmartArgBindingResolver : IArgBindingResolver
{
    private static readonly Regex FullTokenRegex = new("^\\$\\{([^}]+)\\}$", RegexOptions.Compiled);
    private static readonly Regex InterpolateRegex = new("\\{\\{([^}]+)\\}\\}", RegexOptions.Compiled);

    public Dictionary<string, object?> Resolve(string tool, Dictionary<string, object?> args, IReadOnlyDictionary<string, JsonElement> toolResults)
    {
        var resolved = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in args)
        {
            resolved[k] = ResolveValue(v, toolResults);
        }
        return resolved;
    }

    private static object? ResolveValue(object? value, IReadOnlyDictionary<string, JsonElement> results)
    {
        if (value is null) return null;

        if (value is JsonElement je)
            return ResolveJsonElement(je, results);

        if (value is Dictionary<string, object?> dict)
        {
            var outDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (k, v) in dict) outDict[k] = ResolveValue(v, results);
            return outDict;
        }

        if (value is IEnumerable<object?> arr && value is not string)
            return arr.Select(x => ResolveValue(x, results)).ToList();

        if (value is string s)
            return ResolveString(s, results);

        return value;
    }

    private static object? ResolveJsonElement(JsonElement value, IReadOnlyDictionary<string, JsonElement> results)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Object => value.EnumerateObject()
                .ToDictionary(p => p.Name, p => ResolveJsonElement(p.Value, results), StringComparer.OrdinalIgnoreCase),
            JsonValueKind.Array => value.EnumerateArray().Select(x => ResolveJsonElement(x, results)).ToList(),
            JsonValueKind.String => ResolveString(value.GetString() ?? "", results),
            JsonValueKind.Number => value.TryGetInt64(out var i) ? i : value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value.ToString()
        };
    }

    private static object? ResolveString(string source, IReadOnlyDictionary<string, JsonElement> results)
    {
        if (string.IsNullOrWhiteSpace(source)) return source;

        var fullToken = FullTokenRegex.Match(source);
        if (fullToken.Success)
        {
            return ResolveToken(fullToken.Groups[1].Value.Trim(), results) ?? source;
        }

        if (!InterpolateRegex.IsMatch(source)) return source;

        var rendered = InterpolateRegex.Replace(source, m =>
        {
            var token = m.Groups[1].Value.Trim();
            var value = ResolveToken(token, results);
            return value is null ? "" : Convert.ToString(value) ?? "";
        });

        return rendered;
    }

    // Supported token forms:
    // - tool:shop.searchProducts.products.0.id
    // - step:step_2.items.0.id
    // - shop.searchProducts.products.0.id (direct alias)
    private static object? ResolveToken(string token, IReadOnlyDictionary<string, JsonElement> results)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        if (token.StartsWith("tool:", StringComparison.OrdinalIgnoreCase))
            return ResolveByKeyAndPath(token[5..], results);

        if (token.StartsWith("step:", StringComparison.OrdinalIgnoreCase))
            return ResolveByKeyAndPath(token[5..], results);

        return ResolveByKeyAndPath(token, results);
    }

    private static object? ResolveByKeyAndPath(string keyAndPath, IReadOnlyDictionary<string, JsonElement> results)
    {
        var parts = keyAndPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;

        // Find longest matching key in results (tool names may contain dots)
        for (var i = parts.Length; i >= 1; i--)
        {
            var key = string.Join('.', parts.Take(i));
            if (!results.TryGetValue(key, out var root)) continue;

            var pathParts = parts.Skip(i).ToArray();
            return ExtractFromJson(root, pathParts);
        }

        return null;
    }

    private static object? ExtractFromJson(JsonElement root, IReadOnlyList<string> path)
    {
        var cur = root;
        foreach (var segment in path)
        {
            if (cur.ValueKind == JsonValueKind.Object)
            {
                if (!TryGetPropertyCaseInsensitive(cur, segment, out var next))
                    return null;
                cur = next;
                continue;
            }

            if (cur.ValueKind == JsonValueKind.Array)
            {
                if (!int.TryParse(segment, out var idx)) return null;
                var arr = cur.EnumerateArray().ToArray();
                if (idx < 0 || idx >= arr.Length) return null;
                cur = arr[idx];
                continue;
            }

            return null;
        }

        return cur.ValueKind switch
        {
            JsonValueKind.String => cur.GetString(),
            JsonValueKind.Number => cur.TryGetInt64(out var i) ? i : cur.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => JsonSerializer.Deserialize<object>(cur.GetRawText())
        };
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement obj, string prop, out JsonElement value)
    {
        foreach (var p in obj.EnumerateObject())
        {
            if (string.Equals(p.Name, prop, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        }
        value = default;
        return false;
    }
}
