using FluentAssertions;
using MIBO.Storage.Mongo.Conversations;
using MIBO.Storage.Mongo.Store.Conversation;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace MIBO.Storage.Mongo.Tests.Unit.Store;

public class MongoConversationStoreTests
{
    private readonly Mock<IMongoCollection<ConversationDoc>> _convCollection = new();
    private readonly Mock<IMongoCollection<MessageDoc>> _msgCollection = new();
    private readonly Mock<IMongoCollection<UiInstanceDoc>> _uiCollection = new();
    private readonly MongoConversationStore _sut;

    public MongoConversationStoreTests()
    {
        var dbMock = new Mock<IMongoDatabase>();
        var options = Options.Create(new MongoOptions
        {
            ConversationsCollection = "conversations",
            MessagesCollection = "messages",
            UiInstancesCollection = "ui_instances",
            PlannerContextMessages = 12
        });

        dbMock.Setup(x => x.GetCollection<ConversationDoc>("conversations", null))
            .Returns(_convCollection.Object);
        dbMock.Setup(x => x.GetCollection<MessageDoc>("messages", null))
            .Returns(_msgCollection.Object);
        dbMock.Setup(x => x.GetCollection<UiInstanceDoc>("ui_instances", null))
            .Returns(_uiCollection.Object);

        _sut = new MongoConversationStore(dbMock.Object, options);
    }

    // ════════════════════════════════════════════
    //  CreateConversationAsync
    // ════════════════════════════════════════════

    #region CreateConversationAsync

    [Fact]
    public async Task CreateConversationAsync_InsertsDocumentAndReturnsSummary()
    {
        // Arrange
        _convCollection
            .Setup(x => x.InsertOneAsync(
                It.IsAny<ConversationDoc>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateConversationAsync("user-1", "My Chat", CancellationToken.None);

        // Assert
        result.UserId.Should().Be("user-1");
        result.Title.Should().Be("My Chat");
        result.MessageCount.Should().Be(0);
        result.ConversationId.Should().NotBeNullOrEmpty();
        result.LastMessageAt.Should().BeNull();

        _convCollection.Verify(
            x => x.InsertOneAsync(
                It.Is<ConversationDoc>(d => d.UserId == "user-1" && d.Title == "My Chat"),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateConversationAsync_NullTitle_DefaultsToNewChat()
    {
        // Arrange
        _convCollection
            .Setup(x => x.InsertOneAsync(
                It.IsAny<ConversationDoc>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateConversationAsync("user-1", null, CancellationToken.None);

        // Assert
        result.Title.Should().Be("New chat");
    }

    [Fact]
    public async Task CreateConversationAsync_LongTitle_TruncatesTo80Chars()
    {
        // Arrange
        var longTitle = new string('A', 120);
        _convCollection
            .Setup(x => x.InsertOneAsync(
                It.IsAny<ConversationDoc>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateConversationAsync("user-1", longTitle, CancellationToken.None);

        // Assert
        result.Title.Should().HaveLength(80);
    }

    #endregion

    // ════════════════════════════════════════════
    //  ListConversationsAsync
    // ════════════════════════════════════════════

    #region ListConversationsAsync

    [Fact]
    public async Task ListConversationsAsync_ReturnsConversationsForUser()
    {
        // Arrange
        var docs = new List<ConversationDoc>
        {
            CreateConversationDoc("c1", "user-1", "Chat 1"),
            CreateConversationDoc("c2", "user-1", "Chat 2")
        };

        SetupFindConversations(docs);

        // Act
        var result = await _sut.ListConversationsAsync("user-1", 0, 10, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].ConversationId.Should().Be("c1");
        result[1].ConversationId.Should().Be("c2");
    }

    [Fact]
    public async Task ListConversationsAsync_EmptyList_ReturnsEmpty()
    {
        // Arrange
        SetupFindConversations(new List<ConversationDoc>());

        // Act
        var result = await _sut.ListConversationsAsync("user-1", 0, 10, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListConversationsAsync_NegativeSkip_ClampedToZero()
    {
        // Arrange
        SetupFindConversations(new List<ConversationDoc>());

        // Act — negative skip should not throw
        var act = async () => await _sut.ListConversationsAsync("user-1", -5, 10, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    // ════════════════════════════════════════════
    //  RenameConversationAsync
    // ════════════════════════════════════════════

    #region RenameConversationAsync

    [Fact]
    public async Task RenameConversationAsync_MatchingDoc_ReturnsTrue()
    {
        // Arrange
        _convCollection
            .Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<ConversationDoc>>(),
                It.IsAny<UpdateDefinition<ConversationDoc>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        var result = await _sut.RenameConversationAsync("c1", "user-1", "New Title", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RenameConversationAsync_NoMatch_ReturnsFalse()
    {
        // Arrange
        _convCollection
            .Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<ConversationDoc>>(),
                It.IsAny<UpdateDefinition<ConversationDoc>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

        // Act
        var result = await _sut.RenameConversationAsync("c1", "wrong-user", "Title", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    // ════════════════════════════════════════════
    //  DeleteConversationAsync
    // ════════════════════════════════════════════

    #region DeleteConversationAsync

    [Fact]
    public async Task DeleteConversationAsync_MatchingDoc_DeletesAndReturnsTrue()
    {
        // Arrange
        _convCollection
            .Setup(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<ConversationDoc>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));

        _msgCollection
            .Setup(x => x.DeleteManyAsync(
                It.IsAny<FilterDefinition<MessageDoc>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(5));

        _uiCollection
            .Setup(x => x.DeleteManyAsync(
                It.IsAny<FilterDefinition<UiInstanceDoc>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(0));

        // Act
        var result = await _sut.DeleteConversationAsync("c1", "user-1", CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        // Verify messages and UI instances were also deleted
        _msgCollection.Verify(
            x => x.DeleteManyAsync(It.IsAny<FilterDefinition<MessageDoc>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _uiCollection.Verify(
            x => x.DeleteManyAsync(It.IsAny<FilterDefinition<UiInstanceDoc>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteConversationAsync_NoMatch_ReturnsFalseWithoutDeletingMessages()
    {
        // Arrange
        _convCollection
            .Setup(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<ConversationDoc>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteResult.Acknowledged(0));

        // Act
        var result = await _sut.DeleteConversationAsync("c1", "wrong-user", CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        // Verify messages were NOT deleted (early return)
        _msgCollection.Verify(
            x => x.DeleteManyAsync(It.IsAny<FilterDefinition<MessageDoc>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    // ════════════════════════════════════════════
    //  GetConversationAsync
    // ════════════════════════════════════════════

    #region GetConversationAsync

    [Fact]
    public async Task GetConversationAsync_NotFound_ReturnsNull()
    {
        // Arrange
        SetupFindSingleConversation(null);

        // Act
        var result = await _sut.GetConversationAsync("c1", "user-1", 50, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConversationAsync_WrongUser_ReturnsNull()
    {
        // Arrange — conversation belongs to "user-1" but request is from "user-2"
        var doc = CreateConversationDoc("c1", "user-1", "Chat");
        SetupFindSingleConversation(doc);

        // Act
        var result = await _sut.GetConversationAsync("c1", "user-2", 50, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConversationAsync_ValidOwner_ReturnsDetails()
    {
        // Arrange
        var doc = CreateConversationDoc("c1", "user-1", "Chat");
        SetupFindSingleConversation(doc);
        SetupFindMessages(new List<MessageDoc>
        {
            CreateMessageDoc("m1", "c1", "user-1", "user", "Hello")
        });

        // Act
        var result = await _sut.GetConversationAsync("c1", "user-1", 50, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Conversation.ConversationId.Should().Be("c1");
        result.Messages.Should().HaveCount(1);
        result.Messages[0].Text.Should().Be("Hello");
    }

    [Fact]
    public async Task GetConversationAsync_NoMessages_FallsBackToEmbeddedMessages()
    {
        // Arrange — no messages in messages collection, but conversation has embedded messages
        var doc = CreateConversationDoc("c1", "user-1", "Chat");
        doc.Messages = new List<StoredConversationMessageDoc>
        {
            new()
            {
                Role = "user",
                Content = "Embedded message",
                TimestampUtc = DateTime.UtcNow.ToString("O")
            }
        };
        SetupFindSingleConversation(doc);
        SetupFindMessages(new List<MessageDoc>()); // empty

        // Act
        var result = await _sut.GetConversationAsync("c1", "user-1", 50, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Messages.Should().HaveCount(1);
        result.Messages[0].Text.Should().Be("Embedded message");
    }

    #endregion

    // ════════════════════════════════════════════
    //  AppendUserMessageAsync
    // ════════════════════════════════════════════

    #region AppendUserMessageAsync

    [Fact]
    public async Task AppendUserMessageAsync_InsertsMessageAndTouchesConversation()
    {
        // Arrange — EnsureConversation needs to find existing conversation
        var doc = CreateConversationDoc("c1", "user-1", "New chat");
        SetupFindSingleConversation(doc);

        _msgCollection
            .Setup(x => x.InsertOneAsync(
                It.IsAny<MessageDoc>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _convCollection
            .Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<ConversationDoc>>(),
                It.IsAny<UpdateDefinition<ConversationDoc>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        await _sut.AppendUserMessageAsync("c1", "user-1", "Hello world", "corr-1", CancellationToken.None);

        // Assert — message inserted
        _msgCollection.Verify(
            x => x.InsertOneAsync(
                It.Is<MessageDoc>(m => m.Role == "user" && m.Text == "Hello world" && m.ConversationId == "c1"),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Assert — conversation touched (UpdateOneAsync called for Touch + auto-title)
        _convCollection.Verify(
            x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<ConversationDoc>>(),
                It.IsAny<UpdateDefinition<ConversationDoc>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task AppendUserMessageAsync_DifferentUser_ThrowsOwnershipException()
    {
        // Arrange — conversation belongs to "user-1", trying to append as "user-2"
        var doc = CreateConversationDoc("c1", "user-1", "Chat");
        SetupFindSingleConversation(doc);

        // Act
        var act = async () => await _sut.AppendUserMessageAsync("c1", "user-2", "Hello", "corr-1", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConversationOwnershipException>();
    }

    [Fact]
    public async Task AppendUserMessageAsync_ConversationNotFound_UpsertsNew()
    {
        // Arrange — no existing conversation found
        SetupFindSingleConversation(null);

        _convCollection
            .Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<ConversationDoc>>(),
                It.IsAny<UpdateDefinition<ConversationDoc>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        _msgCollection
            .Setup(x => x.InsertOneAsync(
                It.IsAny<MessageDoc>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act — should upsert a new conversation
        await _sut.AppendUserMessageAsync("new-conv", "user-1", "First message", "corr-1", CancellationToken.None);

        // Assert — UpdateOneAsync called with upsert for EnsureConversation
        _convCollection.Verify(
            x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<ConversationDoc>>(),
                It.IsAny<UpdateDefinition<ConversationDoc>>(),
                It.Is<UpdateOptions>(o => o != null && o.IsUpsert),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ════════════════════════════════════════════
    //  AppendAssistantMessageAsync
    // ════════════════════════════════════════════

    #region AppendAssistantMessageAsync

    [Fact]
    public async Task AppendAssistantMessageAsync_InsertsMessageWithPayload()
    {
        // Arrange
        var doc = CreateConversationDoc("c1", "user-1", "Chat");
        SetupFindSingleConversation(doc);

        _msgCollection
            .Setup(x => x.InsertOneAsync(
                It.IsAny<MessageDoc>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _convCollection
            .Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<ConversationDoc>>(),
                It.IsAny<UpdateDefinition<ConversationDoc>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        var payload = new { type = "chart", data = new[] { 1, 2, 3 } };

        // Act
        await _sut.AppendAssistantMessageAsync("c1", "user-1", "Here is your chart", payload, "corr-1", CancellationToken.None);

        // Assert
        _msgCollection.Verify(
            x => x.InsertOneAsync(
                It.Is<MessageDoc>(m =>
                    m.Role == "assistant" &&
                    m.Text == "Here is your chart" &&
                    m.AssistantPayload != null),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AppendAssistantMessageAsync_NullPayload_InsertsWithoutBsonDoc()
    {
        // Arrange
        var doc = CreateConversationDoc("c1", "user-1", "Chat");
        SetupFindSingleConversation(doc);

        _msgCollection
            .Setup(x => x.InsertOneAsync(
                It.IsAny<MessageDoc>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _convCollection
            .Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<ConversationDoc>>(),
                It.IsAny<UpdateDefinition<ConversationDoc>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

        // Act
        await _sut.AppendAssistantMessageAsync("c1", "user-1", "Plain reply", null, "corr-1", CancellationToken.None);

        // Assert
        _msgCollection.Verify(
            x => x.InsertOneAsync(
                It.Is<MessageDoc>(m => m.AssistantPayload == null),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════

    private static ConversationDoc CreateConversationDoc(string id, string userId, string title)
        => new()
        {
            Id = id,
            ConversationId = id,
            UserId = userId,
            Title = title,
            CreatedAtUtc = DateTime.UtcNow.ToString("O"),
            UpdatedAtUtc = DateTime.UtcNow.ToString("O"),
            LastMessageAtUtc = null,
            LastMessagePreview = null,
            MessageCount = 0,
            Messages = new List<StoredConversationMessageDoc>(),
            Meta = new BsonDocument()
        };

    private static MessageDoc CreateMessageDoc(string msgId, string convId, string userId, string role, string text)
        => new()
        {
            MessageId = msgId,
            ConversationId = convId,
            UserId = userId,
            Role = role,
            Text = text,
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Sets up the conversations collection to return a list of docs via the Find fluent chain.
    /// </summary>
    private void SetupFindConversations(List<ConversationDoc> docs)
    {
        var cursorMock = new Mock<IAsyncCursor<ConversationDoc>>();
        cursorMock.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true).Returns(false);
        cursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);
        cursorMock.Setup(x => x.Current).Returns(docs);

        var findFluentMock = new Mock<IAsyncCursorSource<ConversationDoc>>();
        findFluentMock.Setup(x => x.ToCursorAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);

        _convCollection
            .Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<ConversationDoc>>(),
                It.IsAny<FindOptions<ConversationDoc, ConversationDoc>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);
    }

    /// <summary>
    /// Sets up the conversations collection Find to return a single doc (or null).
    /// </summary>
    private void SetupFindSingleConversation(ConversationDoc? doc)
    {
        var list = doc is not null ? new List<ConversationDoc> { doc } : new List<ConversationDoc>();

        var cursorMock = new Mock<IAsyncCursor<ConversationDoc>>();
        cursorMock.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true).Returns(false);
        cursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);
        cursorMock.Setup(x => x.Current).Returns(list);

        _convCollection
            .Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<ConversationDoc>>(),
                It.IsAny<FindOptions<ConversationDoc, ConversationDoc>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);
    }

    /// <summary>
    /// Sets up the messages collection Find to return a list of message docs.
    /// </summary>
    private void SetupFindMessages(List<MessageDoc> docs)
    {
        var cursorMock = new Mock<IAsyncCursor<MessageDoc>>();
        cursorMock.SetupSequence(x => x.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true).Returns(false);
        cursorMock.SetupSequence(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);
        cursorMock.Setup(x => x.Current).Returns(docs);

        _msgCollection
            .Setup(x => x.FindAsync(
                It.IsAny<FilterDefinition<MessageDoc>>(),
                It.IsAny<FindOptions<MessageDoc, MessageDoc>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorMock.Object);
    }
}
