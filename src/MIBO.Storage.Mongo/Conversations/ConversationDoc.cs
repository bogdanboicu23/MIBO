using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MIBO.Storage.Mongo.Conversations;

public sealed class ConversationDoc
{
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("conversationId")] public string ConversationId { get; set; } = default!;
    [BsonElement("userId")] public string UserId { get; set; } = default!;
    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // opțional: tags, tenant, locale
    [BsonElement("meta")] public BsonDocument Meta { get; set; } = new();
}

public sealed class MessageDoc
{
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("messageId")] public string MessageId { get; set; } = default!;
    [BsonElement("conversationId")] public string ConversationId { get; set; } = default!;
    [BsonElement("userId")] public string UserId { get; set; } = default!;

    // "user" | "assistant" | "system"
    [BsonElement("role")] public string Role { get; set; } = default!;

    [BsonElement("text")] public string Text { get; set; } = "";
    [BsonElement("uiV1")] public BsonDocument? UiV1 { get; set; }

    [BsonElement("correlationId")] public string CorrelationId { get; set; } = default!;
    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class UiInstanceDoc
{
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("uiInstanceId")] public string UiInstanceId { get; set; } = default!;
    [BsonElement("conversationId")] public string ConversationId { get; set; } = default!;
    [BsonElement("userId")] public string UserId { get; set; } = default!;

    // ultimul ui.v1 complet (opțional)
    [BsonElement("uiV1")] public BsonDocument? UiV1 { get; set; }

    // subscriptions normalizate (event-driven refresh)
    [BsonElement("subscriptions")] public List<BsonDocument> Subscriptions { get; set; } = new();

    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("updatedAt")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
