using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.DTOs.PlannerContracts;
using MIBO.ConversationService.Services.Tools;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Planner.Validator;

public sealed class PlanValidator : IPlanValidator
{
    private readonly ChatOrchestratorOptions _opt;
    private readonly IToolRegistry _tools;

    public PlanValidator(IOptions<ChatOrchestratorOptions> opt, IToolRegistry tools)
    {
        _opt = opt.Value;
        _tools = tools;
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
    }
}
