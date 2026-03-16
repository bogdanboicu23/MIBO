using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MIBO.Storage.Mongo.Conversations;

[BsonIgnoreExtraElements]
public sealed class ConversationDoc
{
    [BsonId] public string Id { get; set; } = default!;

    [BsonElement("conversationId")] public string ConversationId { get; set; } = default!;
    [BsonElement("userId")] public string UserId { get; set; } = default!;
    [BsonElement("title")] public string Title { get; set; } = "New chat";
    [BsonElement("created_at")] public string CreatedAtUtc { get; set; } = DateTime.UtcNow.ToString("O");
    [BsonElement("updated_at")] public string UpdatedAtUtc { get; set; } = DateTime.UtcNow.ToString("O");
    [BsonElement("last_message_at")] public string? LastMessageAtUtc { get; set; }
    [BsonElement("last_message_preview")] public string? LastMessagePreview { get; set; }
    [BsonElement("message_count")] public long MessageCount { get; set; }
    [BsonElement("summary")] public string? Summary { get; set; }
    [BsonElement("messages")] public List<StoredConversationMessageDoc> Messages { get; set; } = new();
    [BsonElement("meta")] public BsonDocument Meta { get; set; } = new();

    [BsonExtraElements] public BsonDocument? ExtraElements { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class StoredConversationMessageDoc
{
    [BsonElement("role")] public string Role { get; set; } = default!;
    [BsonElement("content")] public string Content { get; set; } = "";
    [BsonElement("timestamp")] public string TimestampUtc { get; set; } = DateTime.UtcNow.ToString("O");
    [BsonElement("tokens_approx")] public int TokensApprox { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class MessageDoc
{
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("messageId")] public string MessageId { get; set; } = default!;
    [BsonElement("conversationId")] public string ConversationId { get; set; } = default!;
    [BsonElement("userId")] public string UserId { get; set; } = default!;
    [BsonElement("role")] public string Role { get; set; } = default!;
    [BsonElement("text")] public string Text { get; set; } = "";
    [BsonElement("uiV1")] public BsonDocument? UiV1 { get; set; }
    [BsonElement("assistantPayload")] public BsonDocument? AssistantPayload { get; set; }
    [BsonElement("correlationId")] public string CorrelationId { get; set; } = default!;
    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
public sealed class UiInstanceDoc
{
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("uiInstanceId")] public string UiInstanceId { get; set; } = default!;
    [BsonElement("conversationId")] public string ConversationId { get; set; } = default!;
    [BsonElement("userId")] public string UserId { get; set; } = default!;
    [BsonElement("uiV1")] public BsonDocument? UiV1 { get; set; }
    [BsonElement("subscriptions")] public List<BsonDocument> Subscriptions { get; set; } = new();
    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("updatedAt")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
