using MIBO.Storage.Mongo.Conversations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MIBO.Storage.Mongo;

public sealed class MongoIndexHostedService : IHostedService
{
    private readonly IMongoDatabase _db;
    private readonly MongoOptions _opt;

    public MongoIndexHostedService(IMongoDatabase db, IOptions<MongoOptions> opt)
    {
        _db = db;
        _opt = opt.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var conv = _db.GetCollection<ConversationDoc>(_opt.ConversationsCollection);
        var msg = _db.GetCollection<MessageDoc>(_opt.MessagesCollection);
        var ui = _db.GetCollection<UiInstanceDoc>(_opt.UiInstancesCollection);

        await CreateOrReplaceNamedIndexAsync(
            conv,
            new CreateIndexModel<ConversationDoc>(
                Builders<ConversationDoc>.IndexKeys
                    .Ascending(x => x.UserId)
                    .Descending(x => x.UpdatedAtUtc),
                new CreateIndexOptions { Name = "ix_conversation_user_updatedAt" }
            ),
            cancellationToken
        );

        await CreateOrReplaceNamedIndexAsync(
            conv,
            new CreateIndexModel<ConversationDoc>(
                Builders<ConversationDoc>.IndexKeys.Ascending(x => x.ConversationId),
                new CreateIndexOptions { Name = "ix_conversation_conversationId" }
            ),
            cancellationToken
        );

        await CreateOrReplaceNamedIndexAsync(
            msg,
            new CreateIndexModel<MessageDoc>(
                Builders<MessageDoc>.IndexKeys
                    .Ascending(x => x.ConversationId)
                    .Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "ix_messages_conversation_createdAt" }
            ),
            cancellationToken
        );

        await CreateOrReplaceNamedIndexAsync(
            msg,
            new CreateIndexModel<MessageDoc>(
                Builders<MessageDoc>.IndexKeys
                    .Ascending(x => x.ConversationId)
                    .Ascending(x => x.UserId)
                    .Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "ix_messages_conv_user_createdAt" }
            ),
            cancellationToken
        );

        await CreateOrReplaceNamedIndexAsync(
            msg,
            new CreateIndexModel<MessageDoc>(
                Builders<MessageDoc>.IndexKeys.Ascending(x => x.MessageId),
                new CreateIndexOptions { Unique = true, Name = "ux_message_id" }
            ),
            cancellationToken
        );

        await CreateOrReplaceNamedIndexAsync(
            ui,
            new CreateIndexModel<UiInstanceDoc>(
                Builders<UiInstanceDoc>.IndexKeys
                    .Ascending(x => x.ConversationId)
                    .Ascending(x => x.UserId),
                new CreateIndexOptions { Name = "ix_ui_instances_conv_user" }
            ),
            cancellationToken
        );

        await CreateOrReplaceNamedIndexAsync(
            ui,
            new CreateIndexModel<UiInstanceDoc>(
                Builders<UiInstanceDoc>.IndexKeys.Ascending(x => x.UiInstanceId),
                new CreateIndexOptions { Unique = true, Name = "ux_ui_instance_id" }
            ),
            cancellationToken
        );
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task CreateOrReplaceNamedIndexAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        CreateIndexModel<TDocument> model,
        CancellationToken cancellationToken)
    {
        var indexName = model.Options?.Name;
        if (string.IsNullOrWhiteSpace(indexName))
            throw new InvalidOperationException("Mongo indexes managed by this service must have explicit names.");

        try
        {
            await collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
        }
        catch (MongoCommandException ex) when (IsIndexDefinitionConflict(ex))
        {
            await collection.Indexes.DropOneAsync(indexName, cancellationToken);
            await collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
        }
    }

    private static bool IsIndexDefinitionConflict(MongoCommandException ex)
    {
        return ex.Code is 85 or 86
            || ex.Message.Contains("same name as the requested index", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("already exists with a different name", StringComparison.OrdinalIgnoreCase);
    }
}
