using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MIBO.Storage.Mongo.Conversations;

public interface IConversationRepository
{
    Task AppendMessageAsync(MessageDoc msg, CancellationToken ct);
}

public sealed class ConversationRepository : IConversationRepository
{
    private readonly IMongoCollection<MessageDoc> _messages;

    public ConversationRepository(IMongoDatabase db, IOptions<MongoOptions> options)
        => _messages = db.GetCollection<MessageDoc>(options.Value.MessagesCollection);

    public Task AppendMessageAsync(MessageDoc msg, CancellationToken ct)
        => _messages.InsertOneAsync(msg, cancellationToken: ct);
}
