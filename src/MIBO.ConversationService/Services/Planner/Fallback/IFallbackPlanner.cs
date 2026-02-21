using MIBO.ConversationService.DTOs.PlannerContracts;

namespace MIBO.ConversationService.Services.Planner.Fallback;

public interface IFallbackPlanner
{
    ToolPlanV1 CreateFallbackPlan(string userPrompt);
}