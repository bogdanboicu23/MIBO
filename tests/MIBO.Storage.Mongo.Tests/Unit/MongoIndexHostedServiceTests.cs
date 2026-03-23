using FluentAssertions;
using MIBO.Storage.Mongo.Conversations;
using MIBO.Storage.Mongo.Integrations;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace MIBO.Storage.Mongo.Tests.Unit;

public class MongoIndexHostedServiceTests
{
    private readonly Mock<IMongoDatabase> _dbMock;
    private readonly Mock<IMongoIndexManager<ConversationDoc>> _convIndexMock;
    private readonly Mock<IMongoIndexManager<MessageDoc>> _msgIndexMock;
    private readonly Mock<IMongoIndexManager<UiInstanceDoc>> _uiIndexMock;
    private readonly Mock<IMongoIndexManager<ExternalServiceAuditDoc>> _auditIndexMock;
    private readonly MongoIndexHostedService _sut;

    public MongoIndexHostedServiceTests()
    {
        _dbMock = new Mock<IMongoDatabase>();

        var convCollectionMock = new Mock<IMongoCollection<ConversationDoc>>();
        var msgCollectionMock = new Mock<IMongoCollection<MessageDoc>>();
        var uiCollectionMock = new Mock<IMongoCollection<UiInstanceDoc>>();
        var auditCollectionMock = new Mock<IMongoCollection<ExternalServiceAuditDoc>>();

        _convIndexMock = new Mock<IMongoIndexManager<ConversationDoc>>();
        _msgIndexMock = new Mock<IMongoIndexManager<MessageDoc>>();
        _uiIndexMock = new Mock<IMongoIndexManager<UiInstanceDoc>>();
        _auditIndexMock = new Mock<IMongoIndexManager<ExternalServiceAuditDoc>>();

        convCollectionMock.Setup(c => c.Indexes).Returns(_convIndexMock.Object);
        msgCollectionMock.Setup(c => c.Indexes).Returns(_msgIndexMock.Object);
        uiCollectionMock.Setup(c => c.Indexes).Returns(_uiIndexMock.Object);
        auditCollectionMock.Setup(c => c.Indexes).Returns(_auditIndexMock.Object);

        _dbMock.Setup(db => db.GetCollection<ConversationDoc>(It.IsAny<string>(), null))
            .Returns(convCollectionMock.Object);
        _dbMock.Setup(db => db.GetCollection<MessageDoc>(It.IsAny<string>(), null))
            .Returns(msgCollectionMock.Object);
        _dbMock.Setup(db => db.GetCollection<UiInstanceDoc>(It.IsAny<string>(), null))
            .Returns(uiCollectionMock.Object);
        _dbMock.Setup(db => db.GetCollection<ExternalServiceAuditDoc>(It.IsAny<string>(), null))
            .Returns(auditCollectionMock.Object);

        var options = Options.Create(new MongoOptions
        {
            ConversationsCollection = "conversations",
            MessagesCollection = "messages",
            UiInstancesCollection = "ui_instances",
            ExternalServiceAuditsCollection = "external_service_audits"
        });

        _sut = new MongoIndexHostedService(_dbMock.Object, options);
    }

    // ════════════════════════════════════════════
    //  StartAsync — index creation
    // ════════════════════════════════════════════

    [Fact]
    public async Task StartAsync_CreatesConversationIndexes()
    {
        _convIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<ConversationDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        _msgIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<MessageDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        _uiIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<UiInstanceDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        _auditIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<ExternalServiceAuditDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        await _sut.StartAsync(CancellationToken.None);

        _convIndexMock.Verify(
            i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<ConversationDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task StartAsync_CreatesMessageIndexes()
    {
        _convIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<ConversationDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        _msgIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<MessageDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        _uiIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<UiInstanceDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        _auditIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<ExternalServiceAuditDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        await _sut.StartAsync(CancellationToken.None);

        _msgIndexMock.Verify(
            i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<MessageDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task StartAsync_CreatesUiIndexes()
    {
        _convIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<ConversationDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        _msgIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<MessageDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        _uiIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<UiInstanceDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        _auditIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<ExternalServiceAuditDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        await _sut.StartAsync(CancellationToken.None);

        _uiIndexMock.Verify(
            i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<UiInstanceDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    // ════════════════════════════════════════════
    //  Conflict handling (code 85/86)
    // ════════════════════════════════════════════

    [Fact]
    public async Task StartAsync_OnConflictCode85_DropsAndRecreatesIndex()
    {
        var conflictException = CreateMongoCommandException(85);

        // First call throws conflict, second call (after drop) succeeds
        var callCount = 0;
        _convIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<ConversationDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns<CreateIndexModel<ConversationDoc>, CreateOneIndexOptions, CancellationToken>(
                (model, opts, ct) =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw conflictException;
                    return Task.FromResult("idx");
                });

        _msgIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<MessageDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        _uiIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<UiInstanceDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        _auditIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<ExternalServiceAuditDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        _convIndexMock
            .Setup(i => i.DropOneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.StartAsync(CancellationToken.None);

        _convIndexMock.Verify(
            i => i.DropOneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_OnNonConflictException_Propagates()
    {
        var otherException = CreateMongoCommandException(999);

        _convIndexMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<ConversationDoc>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(otherException);

        var act = () => _sut.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<MongoCommandException>();
    }

    // ════════════════════════════════════════════
    //  StopAsync
    // ════════════════════════════════════════════

    [Fact]
    public async Task StopAsync_CompletesImmediately()
    {
        var task = _sut.StopAsync(CancellationToken.None);

        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    // ════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════

    private static MongoCommandException CreateMongoCommandException(int code)
    {
        var connectionId = new MongoDB.Driver.Core.Connections.ConnectionId(
            new MongoDB.Driver.Core.Servers.ServerId(
                new MongoDB.Driver.Core.Clusters.ClusterId(1),
                new System.Net.DnsEndPoint("localhost", 27017)));

        return new MongoCommandException(
            connectionId,
            $"Index conflict error (code {code})",
            new MongoDB.Bson.BsonDocument(),
            new MongoDB.Bson.BsonDocument("code", code));
    }
}
