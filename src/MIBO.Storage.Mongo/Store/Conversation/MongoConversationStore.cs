using System.Text.Json;
using MIBO.Storage.Mongo.Conversations;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace MIBO.Storage.Mongo.Store.Conversation;

public sealed class MongoConversationStore : IConversationStore
{
    private readonly IMongoCollection<ConversationDoc> _conversations;
    private readonly IMongoCollection<MessageDoc> _messages;
    private readonly IMongoCollection<UiInstanceDoc> _uiInstances;
    private readonly MongoOptions _opt;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public MongoConversationStore(IMongoDatabase db, IOptions<MongoOptions> opt)
    {
        _opt = opt.Value;
        _conversations = db.GetCollection<ConversationDoc>(_opt.ConversationsCollection);
        _messages = db.GetCollection<MessageDoc>(_opt.MessagesCollection);
        _uiInstances = db.GetCollection<UiInstanceDoc>(_opt.UiInstancesCollection);
    }

    public async Task<ConversationSummary> CreateConversationAsync(string userId, string? title, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var conversation = new ConversationDoc
        {
            ConversationId = Guid.NewGuid().ToString("N"),
            UserId = userId,
            Title = NormalizeTitle(title),
            CreatedAt = now,
            UpdatedAt = now,
            LastMessageAt = null,
            LastMessagePreview = null,
            MessageCount = 0,
            Meta = new BsonDocument()
        };

        await _conversations.InsertOneAsync(conversation, cancellationToken: ct);
        return ToSummary(conversation);
    }

    public async Task<IReadOnlyList<ConversationSummary>> ListConversationsAsync(string userId, int skip, int limit, CancellationToken ct)
    {
        var boundedSkip = Math.Max(0, skip);
        var boundedLimit = Math.Clamp(limit, 1, 100);

        var filter = Builders<ConversationDoc>.Filter.Eq(x => x.UserId, userId);
        var list = await _conversations
            .Find(filter)
            .SortByDescending(x => x.UpdatedAt)
            .Skip(boundedSkip)
            .Limit(boundedLimit)
            .ToListAsync(ct);

        return list.Select(ToSummary).ToList();
    }

    public async Task<ConversationDetails?> GetConversationAsync(string conversationId, string userId, int messagesLimit, CancellationToken ct)
    {
        var convFilter = Builders<ConversationDoc>.Filter.Eq(x => x.ConversationId, conversationId) &
                         Builders<ConversationDoc>.Filter.Eq(x => x.UserId, userId);

        var conversation = await _conversations.Find(convFilter).FirstOrDefaultAsync(ct);
        if (conversation is null) return null;

        var boundedMessagesLimit = Math.Clamp(messagesLimit, 1, 500);
        var msgFilter = Builders<MessageDoc>.Filter.Eq(x => x.ConversationId, conversationId) &
                        Builders<MessageDoc>.Filter.Eq(x => x.UserId, userId);

        var messages = await _messages
            .Find(msgFilter)
            .SortByDescending(x => x.CreatedAt)
            .Limit(boundedMessagesLimit)
            .ToListAsync(ct);

        messages.Reverse();
        var mapped = messages.Select(ToMessage).ToList();

        return new ConversationDetails(ToSummary(conversation), mapped);
    }

    public async Task<bool> RenameConversationAsync(string conversationId, string userId, string title, CancellationToken ct)
    {
        var filter = Builders<ConversationDoc>.Filter.Eq(x => x.ConversationId, conversationId) &
                     Builders<ConversationDoc>.Filter.Eq(x => x.UserId, userId);

        var update = Builders<ConversationDoc>.Update
            .Set(x => x.Title, NormalizeTitle(title))
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var res = await _conversations.UpdateOneAsync(filter, update, cancellationToken: ct);
        return res.ModifiedCount > 0;
    }

    public async Task<bool> DeleteConversationAsync(string conversationId, string userId, CancellationToken ct)
    {
        var convFilter = Builders<ConversationDoc>.Filter.Eq(x => x.ConversationId, conversationId) &
                         Builders<ConversationDoc>.Filter.Eq(x => x.UserId, userId);

        var deleted = await _conversations.DeleteOneAsync(convFilter, ct);
        if (deleted.DeletedCount == 0) return false;

        var msgFilter = Builders<MessageDoc>.Filter.Eq(x => x.ConversationId, conversationId) &
                        Builders<MessageDoc>.Filter.Eq(x => x.UserId, userId);
        var uiFilter = Builders<UiInstanceDoc>.Filter.Eq(x => x.ConversationId, conversationId) &
                       Builders<UiInstanceDoc>.Filter.Eq(x => x.UserId, userId);

        await _messages.DeleteManyAsync(msgFilter, ct);
        await _uiInstances.DeleteManyAsync(uiFilter, ct);

        return true;
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
        await TouchConversationAsync(conversationId, userId, text, ct);
        await TryAutoSetConversationTitleAsync(conversationId, userId, text, ct);
    }

    public async Task AppendAssistantMessageAsync(string conversationId, string userId, string text, object? uiV1, string correlationId, CancellationToken ct)
    {
        await EnsureConversationAsync(conversationId, userId, ct);

        BsonDocument? uiDoc = null;
        if (uiV1 is not null)
        {
            var json = JsonSerializer.Serialize(uiV1, JsonOpts);
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
        await TouchConversationAsync(conversationId, userId, text, ct);
    }

    public async Task<Dictionary<string, object?>> GetPlannerConversationContextAsync(string conversationId, string userId, CancellationToken ct)
    {
        var n = Math.Max(1, _opt.PlannerContextMessages);

        var filter = Builders<MessageDoc>.Filter.Eq(x => x.ConversationId, conversationId) &
                     Builders<MessageDoc>.Filter.Eq(x => x.UserId, userId);

        var last = await _messages
            .Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Limit(n)
            .ToListAsync(ct);

        var latestAssistantUi = last
            .Where(x => x.Role == "assistant" && x.UiV1 is not null)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToUiObject(x.UiV1))
            .FirstOrDefault();

        last.Reverse();

        var compactMessages = last.Select(x => new Dictionary<string, object?>
        {
            ["role"] = x.Role,
            ["text"] = x.Text,
            ["createdAt"] = x.CreatedAt
        }).ToList();

        var summary = string.Join(" | ", compactMessages.Select(m => $"{m["role"]}: {m["text"]}"));
        if (summary.Length > 1200) summary = summary[..1200];

        return new Dictionary<string, object?>
        {
            ["conversationId"] = conversationId,
            ["userId"] = userId,
            ["lastMessages"] = compactMessages,
            ["lastAssistantUi"] = latestAssistantUi,
            ["memorySummary"] = summary
        };
    }

    private async Task EnsureConversationAsync(string conversationId, string userId, CancellationToken ct)
    {
        var filter = Builders<ConversationDoc>.Filter.Eq(x => x.ConversationId, conversationId) &
                     Builders<ConversationDoc>.Filter.Eq(x => x.UserId, userId);

        var now = DateTime.UtcNow;
        var update = Builders<ConversationDoc>.Update
            .SetOnInsert(x => x.ConversationId, conversationId)
            .SetOnInsert(x => x.UserId, userId)
            .SetOnInsert(x => x.Title, "New chat")
            .SetOnInsert(x => x.CreatedAt, now)
            .SetOnInsert(x => x.UpdatedAt, now)
            .SetOnInsert(x => x.LastMessageAt, null)
            .SetOnInsert(x => x.LastMessagePreview, null)
            .SetOnInsert(x => x.MessageCount, 0)
            .SetOnInsert(x => x.Meta, new BsonDocument());

        await _conversations.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);
    }

    private async Task TouchConversationAsync(string conversationId, string userId, string text, CancellationToken ct)
    {
        var filter = Builders<ConversationDoc>.Filter.Eq(x => x.ConversationId, conversationId) &
                     Builders<ConversationDoc>.Filter.Eq(x => x.UserId, userId);

        var now = DateTime.UtcNow;
        var update = Builders<ConversationDoc>.Update
            .Set(x => x.UpdatedAt, now)
            .Set(x => x.LastMessageAt, now)
            .Set(x => x.LastMessagePreview, MakePreview(text))
            .Inc(x => x.MessageCount, 1);

        await _conversations.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    private async Task TryAutoSetConversationTitleAsync(string conversationId, string userId, string text, CancellationToken ct)
    {
        var filter = Builders<ConversationDoc>.Filter.Eq(x => x.ConversationId, conversationId) &
                     Builders<ConversationDoc>.Filter.Eq(x => x.UserId, userId);

        var current = await _conversations.Find(filter).FirstOrDefaultAsync(ct);
        if (current is null) return;

        if (!string.Equals(current.Title, "New chat", StringComparison.OrdinalIgnoreCase))
            return;

        var title = NormalizeTitle(text);
        var update = Builders<ConversationDoc>.Update.Set(x => x.Title, title);
        await _conversations.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    private static string NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "New chat";
        var normalized = title.Trim();
        return normalized.Length <= 80 ? normalized : normalized[..80];
    }

    private static string MakePreview(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var t = text.Trim();
        return t.Length <= 180 ? t : t[..180];
    }

    private static ConversationSummary ToSummary(ConversationDoc x)
        => new(
            x.ConversationId,
            x.UserId,
            x.Title,
            x.CreatedAt,
            x.UpdatedAt,
            x.LastMessageAt,
            x.LastMessagePreview,
            x.MessageCount
        );

    private static ConversationMessage ToMessage(MessageDoc x)
        => new(
            x.MessageId,
            x.ConversationId,
            x.UserId,
            x.Role,
            x.Text,
            ToUiObject(x.UiV1),
            x.CorrelationId,
            x.CreatedAt
        );

    private static object? ToUiObject(BsonDocument? uiDoc)
    {
        if (uiDoc is null) return null;

        var json = uiDoc.ToJson(new JsonWriterSettings
        {
            OutputMode = JsonOutputMode.RelaxedExtendedJson
        });

        return JsonSerializer.Deserialize<object>(json, JsonOpts);
    }
}
