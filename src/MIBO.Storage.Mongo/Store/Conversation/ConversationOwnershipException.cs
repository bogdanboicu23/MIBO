namespace MIBO.Storage.Mongo.Store.Conversation;

public sealed class ConversationOwnershipException(string conversationId)
    : Exception($"Conversation '{conversationId}' is owned by a different user.")
{
    public string ConversationId { get; } = conversationId;
}
