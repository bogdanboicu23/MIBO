using MIBO.ConversationService.DTOs.PlannerContracts;

namespace MIBO.ConversationService.Services.Planner.Client;

public interface IPlannerClient
{
    Task<ToolPlanV1> PlanAsync(object plannerInput, CancellationToken ct);
}
