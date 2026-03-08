using System.Text.Json;
using System.Text.RegularExpressions;

namespace MIBO.ConversationService.Services.UI.Validation;

public sealed class UiContractValidator : IUiContractValidator
{
    private static readonly Regex InlineTemplateRegex = new(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);
    private static readonly Regex FullTokenRegex = new(@"^\$\{([^}]+)\}$", RegexOptions.Compiled);

    public UiValidationResult Validate(object uiV1)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var knownPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(JsonSerializer.Serialize(uiV1, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        }
        catch (Exception ex)
        {
            return new UiValidationResult(
                false,
                new[] { $"ui_parse_error:{ex.Message}" },
                warnings
            );
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                errors.Add("ui_root_must_be_object");

            if (!TryGetString(root, "schema", out var schema) || !string.Equals(schema, "ui.v1", StringComparison.Ordinal))
                errors.Add("ui_schema_must_be_ui.v1");

            if (!root.TryGetProperty("root", out var rootNode))
            {
                errors.Add("ui_root_node_missing");
            }
            else
            {
                ValidateNode(rootNode, "/root", knownPaths, errors);
            }

            ValidateBindings(root, knownPaths, errors, warnings);
            ValidateSubscriptions(root, errors);
        }

        return new UiValidationResult(errors.Count == 0, errors, warnings);
    }

    private static void ValidateNode(
        JsonElement node,
        string path,
        HashSet<string> knownPaths,
        List<string> errors
    )
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"node_not_object:{path}");
            return;
        }

        if (!TryGetString(node, "type", out var type))
        {
            errors.Add($"node_type_missing:{path}");
            return;
        }

        knownPaths.Add(path);

        if (string.Equals(type, "layout", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryGetString(node, "name", out var layoutName))
            {
                errors.Add($"layout_name_missing:{path}");
            }
            else if (layoutName is not ("column" or "row" or "grid"))
            {
                errors.Add($"layout_name_invalid:{path}:{layoutName}");
            }
        }
        else if (string.Equals(type, "component", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryGetString(node, "name", out var componentName) || string.IsNullOrWhiteSpace(componentName))
                errors.Add($"component_name_missing:{path}");
        }
        else
        {
            errors.Add($"node_type_invalid:{path}:{type}");
        }

        if (!node.TryGetProperty("children", out var children))
            return;

        if (children.ValueKind != JsonValueKind.Array)
        {
            errors.Add($"children_must_be_array:{path}");
            return;
        }

        var i = 0;
        foreach (var child in children.EnumerateArray())
        {
            ValidateNode(child, $"{path}/children/{i}", knownPaths, errors);
            i++;
        }
    }

    private static void ValidateBindings(
        JsonElement uiRoot,
        HashSet<string> knownPaths,
        List<string> errors,
        List<string> warnings
    )
    {
        if (!uiRoot.TryGetProperty("bindings", out var bindings)) return;
        if (bindings.ValueKind != JsonValueKind.Array)
        {
            errors.Add("bindings_must_be_array");
            return;
        }

        foreach (var binding in bindings.EnumerateArray())
        {
            if (binding.ValueKind != JsonValueKind.Object)
            {
                errors.Add("binding_must_be_object");
                continue;
            }

            var path = TryGetString(binding, "componentPath", out var componentPath) ? componentPath : "";
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/'))
                errors.Add("binding_componentPath_invalid");
            else if (!knownPaths.Contains(path))
                warnings.Add($"binding_componentPath_not_found:{path}");

            if (!TryGetString(binding, "prop", out var prop) || string.IsNullOrWhiteSpace(prop))
                errors.Add("binding_prop_missing");

            if (!TryGetString(binding, "from", out var from) || string.IsNullOrWhiteSpace(from))
            {
                errors.Add("binding_from_missing");
            }
            else
            {
                ValidateTemplateExpression(from, errors);
            }
        }
    }

    private static void ValidateSubscriptions(JsonElement uiRoot, List<string> errors)
    {
        if (!uiRoot.TryGetProperty("subscriptions", out var subscriptions)) return;
        if (subscriptions.ValueKind != JsonValueKind.Array)
        {
            errors.Add("subscriptions_must_be_array");
            return;
        }

        foreach (var sub in subscriptions.EnumerateArray())
        {
            if (sub.ValueKind != JsonValueKind.Object)
            {
                errors.Add("subscription_must_be_object");
                continue;
            }

            if (!TryGetString(sub, "event", out var eventName) || string.IsNullOrWhiteSpace(eventName))
                errors.Add("subscription_event_missing");

            if (!sub.TryGetProperty("refresh", out var refresh) || refresh.ValueKind != JsonValueKind.Array)
            {
                errors.Add("subscription_refresh_must_be_array");
                continue;
            }

            foreach (var item in refresh.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    errors.Add("subscription_refresh_item_must_be_object");
                    continue;
                }

                if (!TryGetString(item, "tool", out var tool) || string.IsNullOrWhiteSpace(tool))
                    errors.Add("subscription_refresh_tool_missing");

                if (!TryGetString(item, "patchPath", out var patchPath) || !patchPath.StartsWith('/'))
                    errors.Add("subscription_refresh_patchPath_invalid");

                if (item.TryGetProperty("args", out var args))
                    ValidateStringExpressions(args, errors);
            }
        }
    }

    private static void ValidateStringExpressions(JsonElement element, List<string> errors)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            ValidateTemplateExpression(element.GetString() ?? "", errors);
            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                ValidateStringExpressions(item, errors);
            return;
        }

        if (element.ValueKind != JsonValueKind.Object) return;

        foreach (var prop in element.EnumerateObject())
            ValidateStringExpressions(prop.Value, errors);
    }

    private static void ValidateTemplateExpression(string value, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        if (value.Contains("${", StringComparison.Ordinal) && !FullTokenRegex.IsMatch(value))
        {
            errors.Add($"template_token_invalid:{value}");
            return;
        }

        if (FullTokenRegex.IsMatch(value))
        {
            var token = FullTokenRegex.Match(value).Groups[1].Value.Trim();
            if (!IsAllowedToken(token))
                errors.Add($"template_token_disallowed:{token}");
        }

        foreach (Match match in InlineTemplateRegex.Matches(value))
        {
            var token = match.Groups[1].Value.Trim();
            if (!IsAllowedToken(token))
                errors.Add($"template_token_disallowed:{token}");
        }
    }

    private static bool IsAllowedToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        if (token.StartsWith("tool:", StringComparison.OrdinalIgnoreCase)) return true;
        if (token.StartsWith("step:", StringComparison.OrdinalIgnoreCase)) return true;
        if (token.StartsWith("payload.", StringComparison.OrdinalIgnoreCase)) return true;
        if (token.StartsWith("ui.", StringComparison.OrdinalIgnoreCase)) return true;
        if (token.StartsWith("context.", StringComparison.OrdinalIgnoreCase)) return true;

        return Regex.IsMatch(token, "^[a-zA-Z0-9_.:-]+$");
    }

    private static bool TryGetString(JsonElement obj, string prop, out string value)
    {
        value = "";
        if (!obj.TryGetProperty(prop, out var el) || el.ValueKind != JsonValueKind.String)
            return false;
        value = el.GetString() ?? "";
        return true;
    }
}
