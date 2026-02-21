using MIBO.Storage.Mongo.Conversations;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MIBO.Storage.Mongo.Store.UiSubscription;

public sealed class MongoUiSubscriptionStore : IUiSubscriptionStore
{
    private readonly IMongoCollection<UiInstanceDoc> _ui;

    public MongoUiSubscriptionStore(IMongoDatabase db, IOptions<MongoOptions> opt)
        => _ui = db.GetCollection<UiInstanceDoc>(opt.Value.UiInstancesCollection);

    public async Task<IReadOnlyList<AffectedUiInstance>> FindAffectedAsync(
        string eventName,
        string? conversationId,
        string? userId,
        CancellationToken ct
    )
    {
        var filter = Builders<UiInstanceDoc>.Filter.ElemMatch(
            x => x.Subscriptions,
            Builders<BsonDocument>.Filter.Eq("event", eventName)
        );

        if (!string.IsNullOrWhiteSpace(conversationId))
            filter &= Builders<UiInstanceDoc>.Filter.Eq(x => x.ConversationId, conversationId);

        if (!string.IsNullOrWhiteSpace(userId))
            filter &= Builders<UiInstanceDoc>.Filter.Eq(x => x.UserId, userId);

        var list = await _ui.Find(filter)
            .Project(x => new AffectedUiInstance(x.UiInstanceId, x.ConversationId, x.UserId, x.Subscriptions))
            .ToListAsync(ct);

        return list;
    }
}