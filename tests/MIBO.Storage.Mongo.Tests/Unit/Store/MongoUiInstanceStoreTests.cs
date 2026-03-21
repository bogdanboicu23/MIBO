using FluentAssertions;
using MIBO.Storage.Mongo.Conversations;
using MIBO.Storage.Mongo.Store.Ui;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace MIBO.Storage.Mongo.Tests.Unit.Store;

public class MongoUiInstanceStoreTests
{
    private readonly Mock<IMongoCollection<UiInstanceDoc>> _uiCollection = new();
    private readonly MongoUiInstanceStore _sut;

    public MongoUiInstanceStoreTests()
    {
        var dbMock = new Mock<IMongoDatabase>();
        var options = Options.Create(new MongoOptions
        {
            UiInstancesCollection = "ui_instances"
        });

        dbMock.Setup(x => x.GetCollection<UiInstanceDoc>("ui_instances", null))
            .Returns(_uiCollection.Object);

        _sut = new MongoUiInstanceStore(dbMock.Object, options);
    }

    // ════════════════════════════════════════════
    //  UpsertFromAssistantMessageAsync
    // ════════════════════════════════════════════

    #region UpsertFromAssistantMessageAsync

    [Fact]
    public async Task UpsertFromAssistantMessageAsync_UpsertsDocument()
    {
        // Arrange
        _uiCollection
            .Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<UiInstanceDoc>>(),
                It.IsAny<UpdateDefinition<UiInstanceDoc>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        var uiV1 = new { uiInstanceId = "ui-1", type = "chart" };

        // Act
        await _sut.UpsertFromAssistantMessageAsync("conv-1", "user-1", uiV1, CancellationToken.None);

        // Assert
        _uiCollection.Verify(
            x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<UiInstanceDoc>>(),
                It.IsAny<UpdateDefinition<UiInstanceDoc>>(),
                It.Is<UpdateOptions>(o => o.IsUpsert),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpsertFromAssistantMessageAsync_NoUiInstanceId_GeneratesOne()
    {
        // Arrange — uiV1 without uiInstanceId
        _uiCollection
            .Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<UiInstanceDoc>>(),
                It.IsAny<UpdateDefinition<UiInstanceDoc>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        var uiV1 = new { type = "table" }; // no uiInstanceId

        // Act — should not throw
        var act = async () => await _sut.UpsertFromAssistantMessageAsync("conv-1", "user-1", uiV1, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        _uiCollection.Verify(
            x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<UiInstanceDoc>>(),
                It.IsAny<UpdateDefinition<UiInstanceDoc>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpsertFromAssistantMessageAsync_WithSubscriptions_ParsesThem()
    {
        // Arrange
        _uiCollection
            .Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<UiInstanceDoc>>(),
                It.IsAny<UpdateDefinition<UiInstanceDoc>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        var uiV1 = new
        {
            uiInstanceId = "ui-2",
            subscriptions = new[] { new { @event = "finance.expense_created", refresh = new[] { "ds1" } } }
        };

        // Act
        await _sut.UpsertFromAssistantMessageAsync("conv-1", "user-1", uiV1, CancellationToken.None);

        // Assert — called once with upsert
        _uiCollection.Verify(
            x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<UiInstanceDoc>>(),
                It.IsAny<UpdateDefinition<UiInstanceDoc>>(),
                It.Is<UpdateOptions>(o => o.IsUpsert),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ════════════════════════════════════════════
    //  SavePatchAsync
    // ════════════════════════════════════════════

    #region SavePatchAsync

    [Fact]
    public async Task SavePatchAsync_UpdatesTimestamp()
    {
        // Arrange
        _uiCollection
            .Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<UiInstanceDoc>>(),
                It.IsAny<UpdateDefinition<UiInstanceDoc>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        await _sut.SavePatchAsync("ui-1", new { op = "replace" }, CancellationToken.None);

        // Assert
        _uiCollection.Verify(
            x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<UiInstanceDoc>>(),
                It.IsAny<UpdateDefinition<UiInstanceDoc>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}