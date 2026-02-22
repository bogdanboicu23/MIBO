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
        var ui  = _db.GetCollection<UiInstanceDoc>(_opt.UiInstancesCollection);

        // conversations: unique (conversationId,userId)
        await conv.Indexes.CreateOneAsync(
            new CreateIndexModel<ConversationDoc>(
                Builders<ConversationDoc>.IndexKeys
                    .Ascending(x => x.ConversationId)
                    .Ascending(x => x.UserId),
                new CreateIndexOptions { Unique = true, Name = "ux_conversation_user" }
            ),
            cancellationToken: cancellationToken
        );

        // messages: query last messages by conversationId
        await msg.Indexes.CreateOneAsync(
            new CreateIndexModel<MessageDoc>(
                Builders<MessageDoc>.IndexKeys
                    .Ascending(x => x.ConversationId)
                    .Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "ix_messages_conversation_createdAt" }
            ),
            cancellationToken: cancellationToken
        );

        // ui_instances: find by conversationId/userId
        await ui.Indexes.CreateOneAsync(
            new CreateIndexModel<UiInstanceDoc>(
                Builders<UiInstanceDoc>.IndexKeys
                    .Ascending(x => x.ConversationId)
                    .Ascending(x => x.UserId),
                new CreateIndexOptions { Name = "ix_ui_instances_conv_user" }
            ),
            cancellationToken: cancellationToken
        );

        // ui_instances: unique uiInstanceId
        await ui.Indexes.CreateOneAsync(
            new CreateIndexModel<UiInstanceDoc>(
                Builders<UiInstanceDoc>.IndexKeys.Ascending(x => x.UiInstanceId),
                new CreateIndexOptions { Unique = true, Name = "ux_ui_instance_id" }
            ),
            cancellationToken: cancellationToken
        );
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
