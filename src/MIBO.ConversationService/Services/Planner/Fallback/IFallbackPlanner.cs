using MIBO.ConversationService.DTOs.PlannerContracts;

namespace MIBO.ConversationService.Services.Planner.Fallback;

public interface IFallbackPlanner
{
    Task<ToolPlanV1> CreateFallbackPlanAsync(string userPrompt, CancellationToken ct);
}
