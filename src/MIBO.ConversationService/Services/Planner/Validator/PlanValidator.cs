using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.DTOs.PlannerContracts;
using MIBO.ConversationService.Services.Tools;
using MIBO.ConversationService.Services.UI.Validation;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Planner.Validator;

public sealed class PlanValidator : IPlanValidator
{
    private readonly ChatOrchestratorOptions _opt;
    private readonly IToolRegistry _tools;
    private readonly IUiContractValidator _uiValidator;

    public PlanValidator(
        IOptions<ChatOrchestratorOptions> opt,
        IToolRegistry tools,
        IUiContractValidator uiValidator
    )
    {
        _opt = opt.Value;
        _tools = tools;
        _uiValidator = uiValidator;
    }

    public void ValidateOrThrow(ToolPlanV1 plan)
    {
        var registryHasTools = _tools.All().Count > 0;

        if (!string.Equals(plan.Schema, "tool_plan.v1", StringComparison.Ordinal))
            throw new InvalidOperationException($"Invalid plan schema: {plan.Schema}");

        if (plan.Steps.Count > _opt.MaxToolSteps)
            throw new InvalidOperationException($"Plan exceeds MaxToolSteps: {plan.Steps.Count} > {_opt.MaxToolSteps}");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in plan.Steps)
        {
            if (string.IsNullOrWhiteSpace(s.Id))
                throw new InvalidOperationException("Plan step id is empty");

            if (!ids.Add(s.Id))
                throw new InvalidOperationException($"Duplicate step id: {s.Id}");

            if (string.IsNullOrWhiteSpace(s.Tool))
                throw new InvalidOperationException($"Plan step '{s.Id}' tool is empty");

            if (registryHasTools && !_tools.TryGet(s.Tool, out _))
                throw new InvalidOperationException($"Plan step '{s.Id}' references unknown tool '{s.Tool}'");
        }

        if (plan.UiIntent is null || !_opt.StrictUiValidation) return;

        SanitizeUiIntent(plan.UiIntent);

        var draftUi = new Dictionary<string, object?>
        {
            ["schema"] = "ui.v1",
            ["root"] = plan.UiIntent.ComponentTree,
            ["data"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
            ["bindings"] = plan.UiIntent.Bindings,
            ["subscriptions"] = plan.UiIntent.Subscriptions
        };

        var validation = _uiValidator.Validate(draftUi);
        if (!validation.IsValid)
            throw new InvalidOperationException($"Invalid uiIntent in plan: {string.Join(" | ", validation.Errors)}");
    }

    private static void SanitizeUiIntent(UiIntent uiIntent)
    {
        if (uiIntent.Bindings is { Count: > 0 })
        {
            var sanitizedBindings = new List<Dictionary<string, object?>>();
            foreach (var binding in uiIntent.Bindings)
            {
                var path = ReadString(binding, "componentPath");
                var prop = ReadString(binding, "prop");
                var from = ReadString(binding, "from");

                if (string.IsNullOrWhiteSpace(prop) || string.IsNullOrWhiteSpace(from))
                    continue;

                if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/'))
                    path = "/root";

                sanitizedBindings.Add(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["componentPath"] = path,
                    ["prop"] = prop,
                    ["from"] = from
                });
            }

            uiIntent.Bindings = sanitizedBindings;
        }
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value is null) return "";
        return Convert.ToString(value) ?? "";
    }
}
