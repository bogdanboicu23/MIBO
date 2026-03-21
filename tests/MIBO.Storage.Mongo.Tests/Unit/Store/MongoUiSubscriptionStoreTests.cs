using FluentAssertions;
using MIBO.Storage.Mongo.Conversations;
using MIBO.Storage.Mongo.Store.Ui;
using MIBO.Storage.Mongo.Store.UiSubscription;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace MIBO.Storage.Mongo.Tests.Unit.Store;

public class MongoUiSubscriptionStoreTests
{
    private readonly Mock<IMongoCollection<UiInstanceDoc>> _uiCollection = new();
    private readonly MongoUiSubscriptionStore _sut;

    public MongoUiSubscriptionStoreTests()
    {
        var dbMock = new Mock<IMongoDatabase>();
        var options = Options.Create(new MongoOptions
        {
            UiInstancesCollection = "ui_instances"
        });

        dbMock.Setup(x => x.GetCollection<UiInstanceDoc>("ui_instances", null))
            .Returns(_uiCollection.Object);

        _sut = new MongoUiSubscriptionStore(dbMock.Object, options);
    }

    // ════════════════════════════════════════════
    //  FindAffectedAsync
    // ════════════════════════════════════════════

    [Fact]
    public async Task FindAffectedAsync_ReturnsMatchingInstances()
    {
        // Arrange
        var docs = new List<AffectedUiInstance>
        {
            new("ui-1", "conv-1", "user-1", new List<BsonDocument>
            {
                BsonDocument.Parse("""{"event": "finance.expense_created"}""")
            })
        };

        SetupFindAffected(docs);

        // Act
        var result = await _sut.FindAffectedAsync("finance.expense_created", "conv-1", "user-1", CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].UiInstanceId.Should().Be("ui-1");
    }

    [Fact]
    public async Task FindAffectedAsync_NoMatch_ReturnsEmpty()
    {
        // Arrange
        SetupFindAffected(new List<AffectedUiInstance>());

        // Act
        var result = await _sut.FindAffectedAsync("unknown.event", null, null, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindAffectedAsync_NullConversationAndUser_FiltersOnlyByEvent()
    {
        // Arrange
        SetupFindAffected(new List<AffectedUiInstance>());

        // Act — should not throw with null conversationId and userId
        var act = async () => await _sut.FindAffectedAsync("some.event", null, null, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ════════════════════════════════════════════
    //  Model Tests
    // ════════════════════════════════════════════

    [Fact]
    public void AffectedUiInstance_AllFieldsSetViaConstructor()
    {
        var subs = new List<BsonDocument> { new BsonDocument("event", "test") };
        var instance = new AffectedUiInstance("ui-1", "conv-1", "user-1", subs);

        instance.UiInstanceId.Should().Be("ui-1");
        instance.ConversationId.Should().Be("conv-1");
        instance.UserId.Should().Be("user-1");
        instance.Subscriptions.Should().HaveCount(1);
    }

    [Fact]
    public void UiInstanceRef_AllFieldsSetViaConstructor()
    {
        var r = new UiInstanceRef("ui-1", "conv-1", "user-1");

        r.UiInstanceId.Should().Be("ui-1");
        r.ConversationId.Should().Be("conv-1");
        r.UserId.Should().Be("user-1");
    }

    // ════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════

    private void SetupFindAffected(List<AffectedUiInstance> docs)
    {
        var cursorMock = new Mock<IAsyncCursor<AffectedUiInstance>>();
        cursorMock.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true).Returns(false);
        cursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);
        cursorMock.Setup(x => x.Current).Returns(docs);

        _uiCollection
            .Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<UiInstanceDoc>>(),
                It.IsAny<FindOptions<UiInstanceDoc, AffectedUiInstance>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);
    }
}