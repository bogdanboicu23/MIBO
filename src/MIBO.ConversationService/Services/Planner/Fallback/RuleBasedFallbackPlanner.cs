using MIBO.ConversationService.DTOs.PlannerContracts;

namespace MIBO.ConversationService.Services.Planner.Fallback;

public sealed class RuleBasedFallbackPlanner : IFallbackPlanner
{
    public ToolPlanV1 CreateFallbackPlan(string userPrompt)
        => new()
        {
            Schema = "tool_plan.v1",
            Rationale = "Fallback plan (rule-based placeholder).",
            Steps = new(),
            UiIntent = null
        };
}