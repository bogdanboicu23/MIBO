using FluentAssertions;
using MIBO.Storage.Mongo.Conversations;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace MIBO.Storage.Mongo.Tests.Unit.Store;

public class ConversationRepositoryTests
{
    private readonly Mock<IMongoCollection<MessageDoc>> _msgCollection = new();
    private readonly ConversationRepository _sut;

    public ConversationRepositoryTests()
    {
        var dbMock = new Mock<IMongoDatabase>();
        var options = Options.Create(new MongoOptions
        {
            MessagesCollection = "messages"
        });

        dbMock.Setup(x => x.GetCollection<MessageDoc>("messages", null))
            .Returns(_msgCollection.Object);

        _sut = new ConversationRepository(dbMock.Object, options);
    }

    [Fact]
    public async Task AppendMessageAsync_InsertsMessageIntoCollection()
    {
        // Arrange
        _msgCollection
            .Setup(x => x.InsertOneAsync(
                It.IsAny<MessageDoc>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var msg = new MessageDoc
        {
            MessageId = "m1",
            ConversationId = "c1",
            UserId = "u1",
            Role = "user",
            Text = "Test message"
        };

        // Act
        await _sut.AppendMessageAsync(msg, CancellationToken.None);

        // Assert
        _msgCollection.Verify(
            x => x.InsertOneAsync(
                It.Is<MessageDoc>(m => m.MessageId == "m1" && m.Text == "Test message"),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AppendMessageAsync_AssistantMessage_InsertsCorrectly()
    {
        // Arrange
        _msgCollection
            .Setup(x => x.InsertOneAsync(
                It.IsAny<MessageDoc>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var msg = new MessageDoc
        {
            MessageId = "m2",
            ConversationId = "c1",
            UserId = "u1",
            Role = "assistant",
            Text = "Response"
        };

        // Act
        await _sut.AppendMessageAsync(msg, CancellationToken.None);

        // Assert
        _msgCollection.Verify(
            x => x.InsertOneAsync(
                It.Is<MessageDoc>(m => m.Role == "assistant"),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}