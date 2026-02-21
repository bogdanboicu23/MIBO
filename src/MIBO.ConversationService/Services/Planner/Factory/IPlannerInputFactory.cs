namespace MIBO.ConversationService.Services.Planner.Factory;


public interface IPlannerInputFactory
{
    Task<object> BuildAsync(
        string conversationId,
        string userId,
        string userPrompt,
        object conversationContext,
        CancellationToken ct
    );
}