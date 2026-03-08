using MIBO.ConversationService.DTOs.PlannerContracts;

namespace MIBO.ConversationService.Services.Planner.Factory;


public interface IPlannerInputFactory
{
    Task<PlannerInputV1> BuildAsync(
        string conversationId,
        string userId,
        string userPrompt,
        Dictionary<string, object?> conversationContext,
        CancellationToken ct
    );
}
