using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using MIBO.ConversationService.DTOs.PlannerContracts;
using MIBO.ConversationService.DTOs.Tools;
using MIBO.ConversationService.Services.Tools;
using MIBO.ConversationService.Services.UI;

namespace MIBO.ConversationService.Services.Planner.Fallback;

public sealed class RuleBasedFallbackPlanner : IFallbackPlanner
{
    private readonly IToolRegistry _tools;
    private readonly IUiCatalogProvider _uiCatalog;

    public RuleBasedFallbackPlanner(IToolRegistry tools, IUiCatalogProvider uiCatalog)
    {
        _tools = tools;
        _uiCatalog = uiCatalog;
    }

    public async Task<ToolPlanV1> CreateFallbackPlanAsync(string userPrompt, CancellationToken ct)
    {
        var prompt = (userPrompt ?? "").Trim();
        var componentNames = await LoadComponentNamesAsync(ct);

        var selectedTools = SelectTools(prompt, _tools.All()).Take(2).ToList();
        var steps = BuildToolSteps(selectedTools, prompt);
        var primaryTool = steps.FirstOrDefault()?.Tool ?? "";

        var chartData = ExtractLabeledNumbers(prompt)
            .Select(v => new Dictionary<string, object?>
            {
                ["name"] = v.Label,
                ["label"] = v.Label,
                ["value"] = v.Value
            })
            .Cast<object?>()
            .ToList();

        var uiIntent = BuildUiIntent(prompt, componentNames, primaryTool, chartData);

        return new ToolPlanV1
        {
            Schema = "tool_plan.v1",
            Rationale = "Fallback plan (global capability-driven recovery).",
            Steps = steps,
            UiIntent = uiIntent,
            Safety = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["needAssistantAnswer"] = steps.Count == 0 && uiIntent is null,
                ["reason"] = "planner_fallback_capability_driven"
            }
        };
    }

    private async Task<HashSet<string>> LoadComponentNamesAsync(CancellationToken ct)
    {
        try
        {
            var catalog = await _uiCatalog.GetCatalogAsync(ct);
            if (!catalog.TryGetProperty("components", out var components) || components.ValueKind != JsonValueKind.Array)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in components.EnumerateArray())
            {
                if (!c.TryGetProperty("name", out var n) || n.ValueKind != JsonValueKind.String) continue;
                var name = n.GetString();
                if (!string.IsNullOrWhiteSpace(name)) set.Add(name);
            }

            return set;
        }
        catch
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static List<ToolDefinition> SelectTools(string prompt, IEnumerable<ToolDefinition> tools)
    {
        var tokens = Regex.Matches(prompt.ToLowerInvariant(), "[a-z0-9ăâîșț]{3,}")
            .Select(m => m.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var scored = tools
            .Select(t =>
            {
                var text = $"{t.Name} {t.Description}".ToLowerInvariant();
                var score = 0;
                foreach (var token in tokens)
                {
                    if (t.Name.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 5;
                    if (text.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 2;
                }
                return (Tool: t, Score: score);
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Tool.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (scored.Count == 0) return new List<ToolDefinition>();

        var positive = scored.Where(x => x.Score > 0).Select(x => x.Tool).ToList();
        if (positive.Count > 0) return positive;

        var low = prompt.ToLowerInvariant();
        if (ContainsAny(low, "list", "show", "search", "find", "arat", "cauta", "detalii"))
        {
            var fallback = scored
                .Select(x => x.Tool)
                .FirstOrDefault(t =>
                    t.Name.Contains("list", StringComparison.OrdinalIgnoreCase) ||
                    t.Name.Contains("search", StringComparison.OrdinalIgnoreCase));
            if (fallback is not null) return new List<ToolDefinition> { fallback };
        }

        return new List<ToolDefinition>();
    }

    private static List<ToolStep> BuildToolSteps(IEnumerable<ToolDefinition> selectedTools, string prompt)
    {
        var steps = new List<ToolStep>();
        var i = 1;
        foreach (var tool in selectedTools)
        {
            var args = new Dictionary<string, object?>(tool.DefaultArgs, StringComparer.OrdinalIgnoreCase);

            foreach (var required in tool.RequiredArgs)
            {
                if (args.TryGetValue(required, out var existing) && existing is not null)
                    continue;

                var inferred = InferArgValue(required, prompt);
                if (inferred is not null) args[required] = inferred;
            }

            var missingRequired = tool.RequiredArgs.Any(r =>
                !args.TryGetValue(r, out var val) || val is null || string.IsNullOrWhiteSpace(Convert.ToString(val)));
            if (missingRequired) continue;

            steps.Add(new ToolStep
            {
                Id = $"step_{i++}",
                Tool = tool.Name,
                Args = args
            });
        }

        return steps;
    }

    private static object? InferArgValue(string argName, string prompt)
    {
        var name = argName.ToLowerInvariant();
        if (name.Contains("query") || name.Equals("q") || name.Contains("keyword"))
            return prompt;
        if (name.Contains("limit")) return 12;
        if (name.Contains("skip") || name.Contains("offset")) return 0;
        if (name.Contains("page")) return 1;
        if (name.Contains("sortby")) return "rating";
        if (name.Equals("order")) return "desc";
        if (name.Contains("period")) return "last30days";
        if (name.Contains("userid")) return 1;
        if (name.Contains("id")) return 1;
        return null;
    }

    private static UiIntent? BuildUiIntent(
        string prompt,
        HashSet<string> components,
        string primaryTool,
        List<object?> chartData
    )
    {
        var has = (string n) => components.Contains(n);
        var children = new List<Dictionary<string, object?>>();

        if (has("pageTitle"))
        {
            children.Add(Component("pageTitle", new Dictionary<string, object?>
            {
                ["text"] = "Interactive Workspace",
                ["subtitle"] = "Recovered from planner fallback"
            }));
        }

        if (has("formCard"))
        {
            children.Add(Component("formCard", new Dictionary<string, object?>
            {
                ["title"] = "Refine Request",
                ["subtitle"] = "Adjust parameters and submit",
                ["actionType"] = "ui.form.submit",
                ["fields"] = new List<Dictionary<string, object?>>
                {
                    new() { ["name"] = "query", ["label"] = "Query", ["type"] = "text", ["required"] = true, ["defaultValue"] = prompt },
                    new() { ["name"] = "limit", ["label"] = "Limit", ["type"] = "number", ["defaultValue"] = 12 }
                }
            }));
        }

        if (!string.IsNullOrWhiteSpace(primaryTool))
        {
            if (has("searchBar"))
            {
                children.Add(Component("searchBar", new Dictionary<string, object?>
                {
                    ["placeholder"] = "Refine search...",
                    ["actionType"] = "ui.refine"
                }));
            }

            if (has("productCarousel"))
            {
                children.Add(Component("productCarousel", new Dictionary<string, object?>
                {
                    ["title"] = "Results",
                    ["dataKey"] = primaryTool,
                    ["actionType"] = "ui.action"
                }));
            }

            if (has("dataTable"))
            {
                children.Add(Component("dataTable", new Dictionary<string, object?>
                {
                    ["title"] = "Data",
                    ["dataKey"] = primaryTool,
                    ["rowsKey"] = "items",
                    ["pageSize"] = 10
                }));
            }

            if (has("pagination"))
            {
                children.Add(Component("pagination", new Dictionary<string, object?>
                {
                    ["actionType"] = "ui.action",
                    ["totalKey"] = $"{primaryTool}.total",
                    ["limitKey"] = $"{primaryTool}.limit",
                    ["skipKey"] = $"{primaryTool}.skip"
                }));
            }
        }

        if (chartData.Count >= 2)
        {
            if (has("pieChart"))
            {
                children.Add(Component("pieChart", new Dictionary<string, object?>
                {
                    ["title"] = "Distribution",
                    ["data"] = chartData,
                    ["showLegend"] = true
                }));
            }
            else if (has("barChart"))
            {
                children.Add(Component("barChart", new Dictionary<string, object?>
                {
                    ["title"] = "Distribution",
                    ["data"] = chartData,
                    ["showValues"] = true
                }));
            }
        }

        if (children.Count == 0 && has("markdown"))
        {
            children.Add(Component("markdown", new Dictionary<string, object?>
            {
                ["title"] = "Assistant Response",
                ["content"] = "Planner indisponibil temporar. UI-ul a fost reconstruit în mod generic."
            }));
        }

        if (children.Count == 0) return null;

        return new UiIntent
        {
            ComponentTree = Layout(children.ToArray()),
            Bindings = new(),
            Subscriptions = new()
        };
    }

    private static List<(string Label, decimal Value)> ExtractLabeledNumbers(string prompt)
    {
        var values = new List<(string Label, decimal Value)>();
        var matches = Regex.Matches(prompt, @"(?<label>[A-Za-zĂÂÎȘȚăâîșț]+)\s*[:=]?\s*(?<value>\d+(?:[.,]\d+)?)");
        foreach (Match m in matches)
        {
            if (!m.Success) continue;
            var label = m.Groups["label"].Value.Trim();
            var raw = m.Groups["value"].Value.Replace(",", ".");
            if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)) continue;
            if (label.Length < 2) continue;
            values.Add((Capitalize(label), value));
        }

        var reverseMatches = Regex.Matches(prompt, @"(?<value>\d+(?:[.,]\d+)?)\s*[$]?\s*(?<label>[A-Za-zĂÂÎȘȚăâîșț]+)");
        foreach (Match m in reverseMatches)
        {
            if (!m.Success) continue;
            var label = m.Groups["label"].Value.Trim();
            var raw = m.Groups["value"].Value.Replace(",", ".");
            if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)) continue;
            if (label.Length < 2) continue;
            values.Add((Capitalize(label), value));
        }

        return values
            .GroupBy(v => v.Label, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(8)
            .ToList();
    }

    private static bool ContainsAny(string text, params string[] keys)
    {
        foreach (var k in keys)
            if (text.Contains(k, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static string Capitalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        return char.ToUpperInvariant(input[0]) + input[1..];
    }

    private static Dictionary<string, object?> Layout(params Dictionary<string, object?>[] children)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "layout",
            ["name"] = "column",
            ["props"] = new Dictionary<string, object?> { ["gap"] = 12 },
            ["children"] = children.ToList()
        };
    }

    private static Dictionary<string, object?> Component(string name, Dictionary<string, object?> props)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "component",
            ["name"] = name,
            ["props"] = props,
            ["children"] = new List<Dictionary<string, object?>>()
        };
    }
}
