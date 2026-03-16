using System.Globalization;
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
    private static readonly DateTime Epoch = DateTime.UnixEpoch;

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
        var conversationId = Guid.NewGuid().ToString("N");

        var conversation = new ConversationDoc
        {
            Id = conversationId,
            ConversationId = conversationId,
            UserId = userId,
            Title = NormalizeTitle(title),
            CreatedAtUtc = ToUtcString(now),
            UpdatedAtUtc = ToUtcString(now),
            LastMessageAtUtc = null,
            LastMessagePreview = null,
            MessageCount = 0,
            Summary = null,
            Messages = new List<StoredConversationMessageDoc>(),
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
            .SortByDescending(x => x.UpdatedAtUtc)
            .Skip(boundedSkip)
            .Limit(boundedLimit)
            .ToListAsync(ct);

        return list.Select(ToSummary).ToList();
    }

    public async Task<ConversationDetails?> GetConversationAsync(string conversationId, string userId, int messagesLimit, CancellationToken ct)
    {
        var conversation = await _conversations
            .Find(Builders<ConversationDoc>.Filter.Eq(x => x.Id, conversationId))
            .FirstOrDefaultAsync(ct);

        if (conversation is null || !IsConversationOwnedBy(conversation, userId))
        {
            return null;
        }

        var boundedMessagesLimit = Math.Clamp(messagesLimit, 1, 500);
        var messages = await _messages
            .Find(Builders<MessageDoc>.Filter.Eq(x => x.ConversationId, conversationId)
                  & Builders<MessageDoc>.Filter.Eq(x => x.UserId, userId))
            .SortByDescending(x => x.CreatedAt)
            .Limit(boundedMessagesLimit)
            .ToListAsync(ct);

        List<ConversationMessage> mapped;
        if (messages.Count > 0)
        {
            mapped = messages
                .OrderBy(x => x.CreatedAt)
                .Select(ToMessage)
                .ToList();
        }
        else
        {
            mapped = conversation.Messages
                .OrderByDescending(x => ParseUtc(x.TimestampUtc))
                .Take(boundedMessagesLimit)
                .OrderBy(x => ParseUtc(x.TimestampUtc))
                .Select(x => ToMessage(conversation, x))
                .ToList();
        }

        return new ConversationDetails(ToSummary(conversation), mapped);
    }

    public async Task<bool> RenameConversationAsync(string conversationId, string userId, string title, CancellationToken ct)
    {
        var result = await _conversations.UpdateOneAsync(
            Builders<ConversationDoc>.Filter.Eq(x => x.Id, conversationId)
            & Builders<ConversationDoc>.Filter.Eq(x => x.UserId, userId),
            Builders<ConversationDoc>.Update
                .Set(x => x.Title, NormalizeTitle(title))
                .Set(x => x.UpdatedAtUtc, ToUtcString(DateTime.UtcNow)),
            cancellationToken: ct);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteConversationAsync(string conversationId, string userId, CancellationToken ct)
    {
        var deleted = await _conversations.DeleteOneAsync(
            Builders<ConversationDoc>.Filter.Eq(x => x.Id, conversationId)
            & Builders<ConversationDoc>.Filter.Eq(x => x.UserId, userId),
            ct);

        if (deleted.DeletedCount == 0)
        {
            return false;
        }

        await _messages.DeleteManyAsync(
            Builders<MessageDoc>.Filter.Eq(x => x.ConversationId, conversationId)
            & Builders<MessageDoc>.Filter.Eq(x => x.UserId, userId),
            ct);

        await _uiInstances.DeleteManyAsync(
            Builders<UiInstanceDoc>.Filter.Eq(x => x.ConversationId, conversationId)
            & Builders<UiInstanceDoc>.Filter.Eq(x => x.UserId, userId),
            ct);

        return true;
    }

    public async Task AppendUserMessageAsync(string conversationId, string userId, string text, string correlationId, CancellationToken ct)
    {
        await EnsureConversationAsync(conversationId, userId, ct);

        var message = new MessageDoc
        {
            MessageId = Guid.NewGuid().ToString("N"),
            ConversationId = conversationId,
            UserId = userId,
            Role = "user",
            Text = text,
            UiV1 = null,
            AssistantPayload = null,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow
        };

        await _messages.InsertOneAsync(message, cancellationToken: ct);
        await TouchConversationAsync(conversationId, userId, text, incrementMessageCount: true, ct);
        await TryAutoSetConversationTitleAsync(conversationId, userId, text, ct);
    }

    public async Task AppendAssistantMessageAsync(string conversationId, string userId, string text, object? assistantPayload, string correlationId, CancellationToken ct)
    {
        await EnsureConversationAsync(conversationId, userId, ct);

        BsonDocument? assistantPayloadDoc = null;
        if (assistantPayload is not null)
        {
            var json = JsonSerializer.Serialize(assistantPayload, JsonOpts);
            assistantPayloadDoc = BsonDocument.Parse(json);
        }

        var message = new MessageDoc
        {
            MessageId = Guid.NewGuid().ToString("N"),
            ConversationId = conversationId,
            UserId = userId,
            Role = "assistant",
            Text = text,
            UiV1 = null,
            AssistantPayload = assistantPayloadDoc,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow
        };

        await _messages.InsertOneAsync(message, cancellationToken: ct);
        await TouchConversationAsync(conversationId, userId, text, incrementMessageCount: true, ct);
    }

    public async Task<Dictionary<string, object?>> GetPlannerConversationContextAsync(string conversationId, string userId, CancellationToken ct)
    {
        var n = Math.Max(1, _opt.PlannerContextMessages);

        var last = await _messages
            .Find(Builders<MessageDoc>.Filter.Eq(x => x.ConversationId, conversationId)
                  & Builders<MessageDoc>.Filter.Eq(x => x.UserId, userId))
            .SortByDescending(x => x.CreatedAt)
            .Limit(n)
            .ToListAsync(ct);

        if (last.Count == 0)
        {
            var conversation = await _conversations
                .Find(Builders<ConversationDoc>.Filter.Eq(x => x.Id, conversationId)
                      & Builders<ConversationDoc>.Filter.Eq(x => x.UserId, userId))
                .FirstOrDefaultAsync(ct);

            if (conversation is not null)
            {
                var fallbackMessages = conversation.Messages
                    .OrderByDescending(x => ParseUtc(x.TimestampUtc))
                    .Take(n)
                    .OrderBy(x => ParseUtc(x.TimestampUtc))
                    .Select(x =>
                    {
                        var text = x.Content;
                        if (string.Equals(x.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                            && TryParseAssistantPayload(x.Content, out _, out var assistantText))
                        {
                            text = assistantText;
                        }

                        return new Dictionary<string, object?>
                        {
                            ["role"] = x.Role,
                            ["text"] = text,
                            ["createdAt"] = ParseUtc(x.TimestampUtc)
                        };
                    })
                    .ToList();

                var fallbackSummary = string.Join(" | ", fallbackMessages.Select(m => $"{m["role"]}: {m["text"]}"));
                if (fallbackSummary.Length > 1200)
                {
                    fallbackSummary = fallbackSummary[..1200];
                }

                return new Dictionary<string, object?>
                {
                    ["conversationId"] = conversationId,
                    ["userId"] = userId,
                    ["lastMessages"] = fallbackMessages,
                    ["lastAssistantUi"] = null,
                    ["memorySummary"] = fallbackSummary
                };
            }
        }

        var latestAssistantPayload = last
            .Where(x => x.Role == "assistant" && (x.AssistantPayload is not null || x.UiV1 is not null))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToAssistantPayloadObject(x.AssistantPayload) ?? ToUiObject(x.UiV1))
            .FirstOrDefault();

        var compactMessages = last
            .OrderBy(x => x.CreatedAt)
            .Select(x => new Dictionary<string, object?>
            {
                ["role"] = x.Role,
                ["text"] = x.Text,
                ["createdAt"] = x.CreatedAt
            })
            .ToList();

        var summary = string.Join(" | ", compactMessages.Select(m => $"{m["role"]}: {m["text"]}"));
        if (summary.Length > 1200)
        {
            summary = summary[..1200];
        }

        return new Dictionary<string, object?>
        {
            ["conversationId"] = conversationId,
            ["userId"] = userId,
            ["lastMessages"] = compactMessages,
            ["lastAssistantUi"] = latestAssistantPayload,
            ["memorySummary"] = summary
        };
    }

    private async Task EnsureConversationAsync(string conversationId, string userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<ConversationDoc>.Filter.Eq(x => x.Id, conversationId);
        var existing = await _conversations.Find(filter).FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(existing.UserId)
                && !string.Equals(existing.UserId, userId, StringComparison.Ordinal))
            {
                throw new ConversationOwnershipException(conversationId);
            }

            var updates = new List<UpdateDefinition<ConversationDoc>>();

            if (string.IsNullOrWhiteSpace(existing.ConversationId))
            {
                updates.Add(Builders<ConversationDoc>.Update.Set(x => x.ConversationId, conversationId));
            }

            if (string.IsNullOrWhiteSpace(existing.UserId))
            {
                updates.Add(Builders<ConversationDoc>.Update.Set(x => x.UserId, userId));
            }

            if (string.IsNullOrWhiteSpace(existing.Title))
            {
                updates.Add(Builders<ConversationDoc>.Update.Set(x => x.Title, "New chat"));
            }

            if (string.IsNullOrWhiteSpace(existing.CreatedAtUtc))
            {
                updates.Add(Builders<ConversationDoc>.Update.Set(x => x.CreatedAtUtc, ToUtcString(now)));
            }

            if (string.IsNullOrWhiteSpace(existing.UpdatedAtUtc))
            {
                updates.Add(Builders<ConversationDoc>.Update.Set(x => x.UpdatedAtUtc, ToUtcString(now)));
            }

            if (updates.Count > 0)
            {
                await _conversations.UpdateOneAsync(
                    filter,
                    Builders<ConversationDoc>.Update.Combine(updates),
                    cancellationToken: ct);
            }

            return;
        }

        await _conversations.UpdateOneAsync(
            filter,
            Builders<ConversationDoc>.Update
                .SetOnInsert(x => x.Id, conversationId)
                .SetOnInsert(x => x.ConversationId, conversationId)
                .SetOnInsert(x => x.UserId, userId)
                .SetOnInsert(x => x.Title, "New chat")
                .SetOnInsert(x => x.CreatedAtUtc, ToUtcString(now))
                .SetOnInsert(x => x.UpdatedAtUtc, ToUtcString(now))
                .SetOnInsert(x => x.LastMessageAtUtc, null)
                .SetOnInsert(x => x.LastMessagePreview, null)
                .SetOnInsert(x => x.MessageCount, 0)
                .SetOnInsert(x => x.Summary, null)
                .SetOnInsert(x => x.Messages, new List<StoredConversationMessageDoc>())
                .SetOnInsert(x => x.Meta, new BsonDocument()),
            new UpdateOptions { IsUpsert = true },
            ct);
    }

    private async Task TouchConversationAsync(string conversationId, string userId, string text, bool incrementMessageCount, CancellationToken ct)
    {
        var update = Builders<ConversationDoc>.Update
            .Set(x => x.UpdatedAtUtc, ToUtcString(DateTime.UtcNow))
            .Set(x => x.LastMessageAtUtc, ToUtcString(DateTime.UtcNow))
            .Set(x => x.LastMessagePreview, MakePreview(text));

        if (incrementMessageCount)
        {
            update = update.Inc(x => x.MessageCount, 1);
        }

        await _conversations.UpdateOneAsync(
            Builders<ConversationDoc>.Filter.Eq(x => x.Id, conversationId)
            & Builders<ConversationDoc>.Filter.Eq(x => x.UserId, userId),
            update,
            cancellationToken: ct);
    }

    private async Task TryAutoSetConversationTitleAsync(string conversationId, string userId, string text, CancellationToken ct)
    {
        var conversation = await _conversations
            .Find(Builders<ConversationDoc>.Filter.Eq(x => x.Id, conversationId)
                  & Builders<ConversationDoc>.Filter.Eq(x => x.UserId, userId))
            .FirstOrDefaultAsync(ct);

        if (conversation is null || !string.Equals(conversation.Title, "New chat", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await _conversations.UpdateOneAsync(
            Builders<ConversationDoc>.Filter.Eq(x => x.Id, conversationId)
            & Builders<ConversationDoc>.Filter.Eq(x => x.UserId, userId),
            Builders<ConversationDoc>.Update.Set(x => x.Title, NormalizeTitle(text)),
            cancellationToken: ct);
    }

    private static string NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "New chat";
        }

        var normalized = title.Trim();
        return normalized.Length <= 80 ? normalized : normalized[..80];
    }

    private static string MakePreview(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.Trim();
        return normalized.Length <= 180 ? normalized : normalized[..180];
    }

    private static ConversationSummary ToSummary(ConversationDoc conversation)
        => new(
            string.IsNullOrWhiteSpace(conversation.ConversationId) ? conversation.Id : conversation.ConversationId,
            conversation.UserId,
            conversation.Title,
            ParseConversationDate(conversation.CreatedAtUtc, conversation.ExtraElements, "createdAt"),
            ParseConversationDate(conversation.UpdatedAtUtc, conversation.ExtraElements, "updatedAt"),
            ParseConversationNullableDate(conversation.LastMessageAtUtc, conversation.ExtraElements, "lastMessageAt"),
            string.IsNullOrWhiteSpace(conversation.LastMessagePreview)
                ? InferLastPreview(conversation.Messages)
                : conversation.LastMessagePreview,
            conversation.MessageCount > 0 ? conversation.MessageCount : conversation.Messages.Count
        );

    private static ConversationMessage ToMessage(MessageDoc message)
        => new(
            message.MessageId,
            message.ConversationId,
            message.UserId,
            message.Role,
            message.Text,
            ToUiObject(message.UiV1),
            ToAssistantPayloadObject(message.AssistantPayload),
            message.CorrelationId,
            message.CreatedAt
        );

    private static ConversationMessage ToMessage(ConversationDoc conversation, StoredConversationMessageDoc storedMessage)
    {
        var createdAt = ParseUtc(storedMessage.TimestampUtc);
        if (string.Equals(storedMessage.Role, "assistant", StringComparison.OrdinalIgnoreCase)
            && TryParseAssistantPayload(storedMessage.Content, out var payload, out var assistantText))
        {
            return new ConversationMessage(
                Guid.NewGuid().ToString("N"),
                string.IsNullOrWhiteSpace(conversation.ConversationId) ? conversation.Id : conversation.ConversationId,
                conversation.UserId,
                storedMessage.Role,
                assistantText,
                null,
                payload,
                string.Empty,
                createdAt
            );
        }

        return new ConversationMessage(
            Guid.NewGuid().ToString("N"),
            string.IsNullOrWhiteSpace(conversation.ConversationId) ? conversation.Id : conversation.ConversationId,
            conversation.UserId,
            storedMessage.Role,
            storedMessage.Content,
            null,
            null,
            string.Empty,
            createdAt
        );
    }

    private static object? ToUiObject(BsonDocument? uiDoc)
    {
        if (uiDoc is null)
        {
            return null;
        }

        var json = uiDoc.ToJson(new JsonWriterSettings
        {
            OutputMode = JsonOutputMode.RelaxedExtendedJson
        });

        return JsonSerializer.Deserialize<object>(json, JsonOpts);
    }

    private static object? ToAssistantPayloadObject(BsonDocument? assistantPayload)
    {
        if (assistantPayload is null)
        {
            return null;
        }

        var json = assistantPayload.ToJson(new JsonWriterSettings
        {
            OutputMode = JsonOutputMode.RelaxedExtendedJson
        });

        return JsonSerializer.Deserialize<object>(json, JsonOpts);
    }

    private static bool TryParseAssistantPayload(string rawContent, out object? payload, out string text)
    {
        payload = null;
        text = rawContent;

        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(rawContent);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            payload = JsonSerializer.Deserialize<object>(rawContent, JsonOpts);
            if (document.RootElement.TryGetProperty("text", out var textNode) && textNode.ValueKind == JsonValueKind.String)
            {
                text = textNode.GetString() ?? string.Empty;
            }
            else
            {
                text = string.Empty;
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsConversationOwnedBy(ConversationDoc conversation, string userId)
        => string.Equals(conversation.UserId, userId, StringComparison.Ordinal);

    private static DateTime ParseConversationDate(string? rawValue, BsonDocument? extraElements, string legacyField)
    {
        if (TryParseUtc(rawValue, out var parsed))
        {
            return parsed;
        }

        if (extraElements is not null && extraElements.TryGetValue(legacyField, out var legacyValue))
        {
            if (legacyValue.IsValidDateTime)
            {
                return legacyValue.ToUniversalTime();
            }

            if (legacyValue.IsString && TryParseUtc(legacyValue.AsString, out parsed))
            {
                return parsed;
            }
        }

        return Epoch;
    }

    private static DateTime? ParseConversationNullableDate(string? rawValue, BsonDocument? extraElements, string legacyField)
    {
        if (TryParseUtc(rawValue, out var parsed))
        {
            return parsed;
        }

        if (extraElements is not null && extraElements.TryGetValue(legacyField, out var legacyValue))
        {
            if (legacyValue.IsValidDateTime)
            {
                return legacyValue.ToUniversalTime();
            }

            if (legacyValue.IsString && TryParseUtc(legacyValue.AsString, out parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static string? InferLastPreview(IReadOnlyList<StoredConversationMessageDoc> messages)
    {
        var last = messages.LastOrDefault();
        if (last is null)
        {
            return null;
        }

        if (string.Equals(last.Role, "assistant", StringComparison.OrdinalIgnoreCase)
            && TryParseAssistantPayload(last.Content, out _, out var assistantText))
        {
            return MakePreview(assistantText);
        }

        return MakePreview(last.Content);
    }

    private static DateTime ParseUtc(string? rawValue)
        => TryParseUtc(rawValue, out var parsed) ? parsed : Epoch;

    private static bool TryParseUtc(string? rawValue, out DateTime parsed)
    {
        if (DateTime.TryParse(
                rawValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out parsed))
        {
            parsed = parsed.ToUniversalTime();
            return true;
        }

        parsed = Epoch;
        return false;
    }

    private static string ToUtcString(DateTime value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
}
