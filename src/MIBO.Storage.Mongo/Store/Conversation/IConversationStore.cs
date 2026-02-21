namespace MIBO.Storage.Mongo.Store.Conversation;

public interface IConversationStore
{
    Task AppendUserMessageAsync(string conversationId, string userId, string text, string correlationId, CancellationToken ct);
    Task AppendAssistantMessageAsync(string conversationId, string userId, string text, object? uiV1, string correlationId, CancellationToken ct);
    Task<object> GetPlannerConversationContextAsync(string conversationId, string userId, CancellationToken ct);
}