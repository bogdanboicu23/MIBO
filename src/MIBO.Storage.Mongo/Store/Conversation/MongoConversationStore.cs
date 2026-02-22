using MIBO.Storage.Mongo.Conversations;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MIBO.Storage.Mongo.Store.Conversation;

public sealed class MongoConversationStore : IConversationStore
{
    private readonly IMongoCollection<ConversationDoc> _conversations;
    private readonly IMongoCollection<MessageDoc> _messages;
    private readonly MongoOptions _opt;

    public MongoConversationStore(IMongoDatabase db, IOptions<MongoOptions> opt)
    {
        _opt = opt.Value;
        _conversations = db.GetCollection<ConversationDoc>(_opt.ConversationsCollection);
        _messages = db.GetCollection<MessageDoc>(_opt.MessagesCollection);
    }

    public async Task AppendUserMessageAsync(string conversationId, string userId, string text, string correlationId, CancellationToken ct)
    {
        await EnsureConversationAsync(conversationId, userId, ct);

        var msg = new MessageDoc
        {
            MessageId = Guid.NewGuid().ToString("N"),
            ConversationId = conversationId,
            UserId = userId,
            Role = "user",
            Text = text,
            UiV1 = null,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow
        };

        await _messages.InsertOneAsync(msg, cancellationToken: ct);
    }

    public async Task AppendAssistantMessageAsync(string conversationId, string userId, string text, object? uiV1, string correlationId, CancellationToken ct)
    {
        await EnsureConversationAsync(conversationId, userId, ct);

        BsonDocument? uiDoc = null;
        if (uiV1 is not null)
        {
            // safe serialize
            var json = System.Text.Json.JsonSerializer.Serialize(uiV1, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            uiDoc = BsonDocument.Parse(json);
        }

        var msg = new MessageDoc
        {
            MessageId = Guid.NewGuid().ToString("N"),
            ConversationId = conversationId,
            UserId = userId,
            Role = "assistant",
            Text = text,
            UiV1 = uiDoc,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow
        };

        await _messages.InsertOneAsync(msg, cancellationToken: ct);
    }

    public async Task<object> GetPlannerConversationContextAsync(string conversationId, string userId, CancellationToken ct)
    {
        // ia ultimele N mesaje (context pentru planner)
        var n = Math.Max(1, _opt.PlannerContextMessages);

        var filter = Builders<MessageDoc>.Filter.Eq(x => x.ConversationId, conversationId) &
                     Builders<MessageDoc>.Filter.Eq(x => x.UserId, userId);

        var last = await _messages
            .Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Limit(n)
            .Project(x => new
            {
                role = x.Role,
                text = x.Text,
                createdAt = x.CreatedAt
                // nu trimitem ui payload către planner decât dacă vrei explicit
            })
            .ToListAsync(ct);

        last.Reverse(); // chronological

        return new
        {
            conversationId,
            userId,
            lastMessages = last
        };
    }

    private async Task EnsureConversationAsync(string conversationId, string userId, CancellationToken ct)
    {
        var filter = Builders<ConversationDoc>.Filter.Eq(x => x.ConversationId, conversationId) &
                     Builders<ConversationDoc>.Filter.Eq(x => x.UserId, userId);

        var update = Builders<ConversationDoc>.Update
            .SetOnInsert(x => x.ConversationId, conversationId)
            .SetOnInsert(x => x.UserId, userId)
            .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow);

        await _conversations.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);
    }
}
