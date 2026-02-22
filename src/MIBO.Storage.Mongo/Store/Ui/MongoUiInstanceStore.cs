using MIBO.Storage.Mongo.Conversations;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MIBO.Storage.Mongo.Store.Ui;

public sealed class MongoUiInstanceStore : IUiInstanceStore
{
    private readonly IMongoCollection<UiInstanceDoc> _ui;
    private readonly MongoOptions _opt;

    public MongoUiInstanceStore(IMongoDatabase db, IOptions<MongoOptions> opt)
    {
        _opt = opt.Value;
        _ui = db.GetCollection<UiInstanceDoc>(_opt.UiInstancesCollection);
    }

    public async Task UpsertFromAssistantMessageAsync(string conversationId, string userId, object uiV1, CancellationToken ct)
    {
        // uiInstanceId poate veni din uiV1 sau îl generăm aici.
        var json = System.Text.Json.JsonSerializer.Serialize(uiV1, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        var uiDoc = BsonDocument.Parse(json);

        var uiInstanceId = uiDoc.TryGetValue("uiInstanceId", out var v) && v.IsString ? v.AsString : Guid.NewGuid().ToString("N");

        var subs = new List<BsonDocument>();
        if (uiDoc.TryGetValue("subscriptions", out var s) && s.IsBsonArray)
        {
            foreach (var item in s.AsBsonArray)
                if (item.IsBsonDocument) subs.Add(item.AsBsonDocument);
        }

        var filter = Builders<UiInstanceDoc>.Filter.Eq(x => x.UiInstanceId, uiInstanceId);

        var update = Builders<UiInstanceDoc>.Update
            .SetOnInsert(x => x.UiInstanceId, uiInstanceId)
            .Set(x => x.ConversationId, conversationId)
            .Set(x => x.UserId, userId)
            .Set(x => x.UiV1, uiDoc)
            .Set(x => x.Subscriptions, subs)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow);

        await _ui.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);
    }

    public async Task<IReadOnlyList<UiInstanceRef>> FindAffectedAsync(string conversationId, string userId, string @event, CancellationToken ct)
    {
        // subscriptions este o listă de docuri care conțin { event: "finance.expense_created", refresh: [...] }
        var filter = Builders<UiInstanceDoc>.Filter.Eq(x => x.ConversationId, conversationId) &
                     Builders<UiInstanceDoc>.Filter.Eq(x => x.UserId, userId) &
                     Builders<UiInstanceDoc>.Filter.ElemMatch(x => x.Subscriptions, Builders<BsonDocument>.Filter.Eq("event", @event));

        var list = await _ui.Find(filter)
            .Project(x => new UiInstanceRef(x.UiInstanceId, x.ConversationId, x.UserId))
            .ToListAsync(ct);

        return list;
    }

    public async Task SavePatchAsync(string uiInstanceId, object patch, CancellationToken ct)
    {
        // optional: păstrezi patch history într-o colecție separată; aici doar actualizăm updatedAt
        var filter = Builders<UiInstanceDoc>.Filter.Eq(x => x.UiInstanceId, uiInstanceId);
        var update = Builders<UiInstanceDoc>.Update.Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _ui.UpdateOneAsync(filter, update, cancellationToken: ct);
    }
}