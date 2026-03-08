namespace MIBO.Storage.Mongo.Store.Conversation;

public interface IConversationStore
{
    Task<ConversationSummary> CreateConversationAsync(string userId, string? title, CancellationToken ct);
    Task<IReadOnlyList<ConversationSummary>> ListConversationsAsync(string userId, int skip, int limit, CancellationToken ct);
    Task<ConversationDetails?> GetConversationAsync(string conversationId, string userId, int messagesLimit, CancellationToken ct);
    Task<bool> RenameConversationAsync(string conversationId, string userId, string title, CancellationToken ct);
    Task<bool> DeleteConversationAsync(string conversationId, string userId, CancellationToken ct);

    Task AppendUserMessageAsync(string conversationId, string userId, string text, string correlationId, CancellationToken ct);
    Task AppendAssistantMessageAsync(string conversationId, string userId, string text, object? uiV1, string correlationId, CancellationToken ct);
    Task<Dictionary<string, object?>> GetPlannerConversationContextAsync(string conversationId, string userId, CancellationToken ct);
}
