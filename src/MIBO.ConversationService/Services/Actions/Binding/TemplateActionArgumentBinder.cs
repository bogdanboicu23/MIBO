using System.Text.Json;
using System.Text.RegularExpressions;
using MIBO.ConversationService.DTOs.Actions;
using MIBO.ConversationService.DTOs.Tools;

namespace MIBO.ConversationService.Services.Actions.Binding;

public sealed class TemplateActionArgumentBinder : IActionArgumentBinder
{
    private static readonly Regex MustacheToken = new(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);

    public Dictionary<string, object?> Bind(ActionRoute route, ActionEnvelopeV1 action, ToolDefinition toolDefinition)
    {
        var args = new Dictionary<string, object?>(toolDefinition.DefaultArgs, StringComparer.OrdinalIgnoreCase);

        foreach (var (argName, templateValue) in route.ArgTemplate)
            args[argName] = ResolveTemplateValue(templateValue, action);

        if (route.ArgMap.Count > 0)
        {
            foreach (var (payloadKey, toolArgKey) in route.ArgMap)
            {
                if (!TryGetPayloadValue(action.Action.Payload, payloadKey, out var val)) continue;
                args[toolArgKey] = NormalizeValue(val);
            }
        }
        else if (route.ArgTemplate.Count == 0)
        {
            foreach (var (k, v) in action.Action.Payload)
            {
                if (string.Equals(k, "__ui", StringComparison.OrdinalIgnoreCase)) continue;
                args[k] = NormalizeValue(v);
            }
        }

        ApplyCommonAliases(args, action.Action.Payload);
        return args;
    }

    private static void ApplyCommonAliases(
        Dictionary<string, object?> args,
        IReadOnlyDictionary<string, object?> payload
    )
    {
        if (!args.ContainsKey("q") && TryGetPayloadValue(payload, "query", out var queryVal))
            args["q"] = queryVal;

        if (!args.ContainsKey("category") && TryGetPayloadValue(payload, "slug", out var slugVal))
            args["category"] = slugVal;

        if (!args.ContainsKey("id") && TryGetPayloadValue(payload, "productId", out var productId))
            args["id"] = productId;

        if ((!args.ContainsKey("sortBy") || !args.ContainsKey("order")) &&
            TryGetPayloadValue(payload, "sort", out var sortRaw))
        {
            var sort = Convert.ToString(sortRaw) ?? "";
            var normalized = sort.Replace(":", "_", StringComparison.Ordinal);
            var parts = normalized.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                args["sortBy"] = parts[0];
                args["order"] = parts[1];
            }
        }
    }

    private static object? ResolveTemplateValue(object? value, ActionEnvelopeV1 action)
    {
        if (value is null) return null;

        if (value is JsonElement je)
            return ResolveJsonElement(je, action);

        if (value is Dictionary<string, object?> dict)
            return dict.ToDictionary(kv => kv.Key, kv => ResolveTemplateValue(kv.Value, action), StringComparer.OrdinalIgnoreCase);

        if (value is IEnumerable<object?> arr && value is not string)
            return arr.Select(v => ResolveTemplateValue(v, action)).ToList();

        if (value is not string s)
            return value;

        var fullToken = MustacheToken.Match(s.Trim());
        if (fullToken.Success && fullToken.Value.Length == s.Trim().Length)
            return ResolveToken(fullToken.Groups[1].Value.Trim(), action);

        if (!MustacheToken.IsMatch(s)) return s;

        return MustacheToken.Replace(s, match =>
        {
            var token = match.Groups[1].Value.Trim();
            var resolved = ResolveToken(token, action);
            return Convert.ToString(resolved) ?? "";
        });
    }

    private static object? ResolveJsonElement(JsonElement value, ActionEnvelopeV1 action)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Object => value.EnumerateObject()
                .ToDictionary(p => p.Name, p => ResolveJsonElement(p.Value, action), StringComparer.OrdinalIgnoreCase),
            JsonValueKind.Array => value.EnumerateArray().Select(x => ResolveJsonElement(x, action)).ToList(),
            JsonValueKind.String => ResolveTemplateValue(value.GetString(), action),
            JsonValueKind.Number => value.TryGetInt64(out var i) ? i : value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value.ToString()
        };
    }

    private static object? ResolveToken(string token, ActionEnvelopeV1 action)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        if (token.StartsWith("payload.", StringComparison.OrdinalIgnoreCase))
        {
            var key = token[8..];
            if (TryGetPayloadValue(action.Action.Payload, key, out var val))
                return val;
            return null;
        }

        if (token.StartsWith("ui.", StringComparison.OrdinalIgnoreCase))
        {
            var key = token[3..];
            return key.ToLowerInvariant() switch
            {
                "uiinstanceid" => action.UiContext?.UiInstanceId,
                "focusedcomponentid" => action.UiContext?.FocusedComponentId,
                _ => null
            };
        }

        if (token.StartsWith("context.", StringComparison.OrdinalIgnoreCase))
        {
            var key = token[8..];
            return key.ToLowerInvariant() switch
            {
                "conversationid" => action.ConversationId,
                "userid" => action.UserId,
                _ => null
            };
        }

        if (TryGetPayloadValue(action.Action.Payload, token, out var fallbackPayload))
            return fallbackPayload;

        return null;
    }

    private static bool TryGetPayloadValue(IReadOnlyDictionary<string, object?> payload, string keyPath, out object? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(keyPath)) return false;

        var parts = keyPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        object? current = payload;

        foreach (var part in parts)
        {
            if (current is IReadOnlyDictionary<string, object?> ro)
            {
                if (!ro.TryGetValue(part, out current))
                    return false;
                continue;
            }

            if (current is Dictionary<string, object?> dict)
            {
                if (!dict.TryGetValue(part, out current))
                    return false;
                continue;
            }

            if (current is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Object && TryGetPropertyCaseInsensitive(je, part, out var next))
                {
                    current = NormalizeValue(next);
                    continue;
                }
                return false;
            }

            return false;
        }

        value = NormalizeValue(current);
        return true;
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement obj, string prop, out JsonElement value)
    {
        foreach (var p in obj.EnumerateObject())
        {
            if (!string.Equals(p.Name, prop, StringComparison.OrdinalIgnoreCase)) continue;
            value = p.Value;
            return true;
        }
        value = default;
        return false;
    }

    private static object? NormalizeValue(object? value)
    {
        if (value is not JsonElement el) return value;

        return el.ValueKind switch
        {
            JsonValueKind.Object => JsonSerializer.Deserialize<object>(el.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            JsonValueKind.Array => JsonSerializer.Deserialize<object>(el.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var i) ? i : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => el.ToString()
        };
    }
}
