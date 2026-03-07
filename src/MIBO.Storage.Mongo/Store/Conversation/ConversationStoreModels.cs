namespace MIBO.Storage.Mongo.Store.Conversation;

public sealed record ConversationSummary(
    string ConversationId,
    string UserId,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastMessageAt,
    string? LastMessagePreview,
    long MessageCount
);

public sealed record ConversationMessage(
    string MessageId,
    string ConversationId,
    string UserId,
    string Role,
    string Text,
    object? UiV1,
    string CorrelationId,
    DateTime CreatedAt
);

public sealed record ConversationDetails(
    ConversationSummary Conversation,
    IReadOnlyList<ConversationMessage> Messages
);
