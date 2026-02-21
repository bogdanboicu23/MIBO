using MongoDB.Bson;

namespace MIBO.Storage.Mongo.Store.UiSubscription;

public sealed record AffectedUiInstance(
    string UiInstanceId,
    string ConversationId,
    string UserId,
    List<BsonDocument> Subscriptions
);

public interface IUiSubscriptionStore
{
    Task<IReadOnlyList<AffectedUiInstance>> FindAffectedAsync(
        string eventName,
        string? conversationId,
        string? userId,
        CancellationToken ct
    );
}