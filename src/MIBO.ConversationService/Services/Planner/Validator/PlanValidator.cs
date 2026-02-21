using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.DTOs.PlannerContracts;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Planner.Validator;

public sealed class PlanValidator : IPlanValidator
{
    private readonly ChatOrchestratorOptions _opt;

    public PlanValidator(IOptions<ChatOrchestratorOptions> opt) => _opt = opt.Value;

    public void ValidateOrThrow(ToolPlanV1 plan)
    {
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
        }
    }
}