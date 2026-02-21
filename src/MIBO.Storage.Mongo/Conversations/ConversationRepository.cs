using MongoDB.Driver;

namespace MIBO.Storage.Mongo.Conversations;

public interface IConversationRepository
{
    Task AppendMessageAsync(MessageDoc msg, CancellationToken ct);
    // + get conversation context etc.
}

public sealed class ConversationRepository : IConversationRepository
{
    private readonly IMongoCollection<MessageDoc> _messages;

    public ConversationRepository(IMongoDatabase db)
        => _messages = db.GetCollection<MessageDoc>("messages");

    public Task AppendMessageAsync(MessageDoc msg, CancellationToken ct)
        => _messages.InsertOneAsync(msg, cancellationToken: ct);
}