using System.Net;
using System.Text;
using FluentAssertions;
using MIBO.ConversationService.Controllers;
using MIBO.Storage.Mongo.Store.Conversation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;

namespace MIBO.ConversationService.Tests.Unit.Controllers;


public class ChatControllerTests
{
    private readonly Mock<IConversationStore> _storeMock;
    private readonly Mock<IHttpClientFactory> _httpFactoryMock;
    private readonly ChatController _sut;

    public ChatControllerTests()
    {
        _storeMock = new Mock<IConversationStore>();
        _httpFactoryMock = new Mock<IHttpClientFactory>();

        _sut = new ChatController(
            _httpFactoryMock.Object,
            _storeMock.Object,
            Mock.Of<ILogger<ChatController>>());

        // Every controller needs an HttpContext
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // ──── Helpers ────

    private static ConversationSummary CreateSummary(string id = "c1", string title = "Test Chat") =>
        new(id, "anonymous", title, DateTime.UtcNow, DateTime.UtcNow, null, null, 0);

    private void SetHeaderUserId(string userId)
    {
        _sut.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = userId;
    }

    private void SetQueryUserId(string userId)
    {
        _sut.ControllerContext.HttpContext.Request.QueryString = new QueryString($"?userId={userId}");
    }

    private void SetupStreamingContext()
    {
        // Replace DefaultHttpContext with one that has a writable response body
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        _sut.ControllerContext = new ControllerContext { HttpContext = context };
    }

    private void SetupAgentClient(HttpStatusCode statusCode, string responseBody)
    {
        var handler = new MockAgentHttpHandler(statusCode, responseBody);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://agent-service") };
        _httpFactoryMock.Setup(x => x.CreateClient("agent")).Returns(client);
    }

    // ════════════════════════════════════════════
    //  LIST CONVERSATIONS
    // ════════════════════════════════════════════

    #region ListConversations

    [Fact]
    public async Task ListConversations_ReturnsOkWithList()
    {
        var conversations = new List<ConversationSummary> { CreateSummary("c1"), CreateSummary("c2") };
        _storeMock
            .Setup(x => x.ListConversationsAsync("anonymous", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversations);

        var result = await _sut.ListConversations();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(conversations);
    }

    [Fact]
    public async Task ListConversations_UsesXUserIdHeader()
    {
        SetHeaderUserId("user-42");
        _storeMock
            .Setup(x => x.ListConversationsAsync("user-42", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversationSummary>());

        await _sut.ListConversations();

        _storeMock.Verify(
            x => x.ListConversationsAsync("user-42", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListConversations_PassesSkipAndLimit()
    {
        _storeMock
            .Setup(x => x.ListConversationsAsync("anonymous", 10, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversationSummary>());

        await _sut.ListConversations(skip: 10, limit: 25);

        _storeMock.Verify(
            x => x.ListConversationsAsync("anonymous", 10, 25, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListConversations_DefaultsToAnonymous()
    {
        _storeMock
            .Setup(x => x.ListConversationsAsync("anonymous", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversationSummary>());

        await _sut.ListConversations();

        _storeMock.Verify(
            x => x.ListConversationsAsync("anonymous", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ════════════════════════════════════════════
    //  CREATE CONVERSATION
    // ════════════════════════════════════════════

    #region CreateConversation

    [Fact]
    public async Task CreateConversation_ReturnsOkWithSummary()
    {
        var summary = CreateSummary("new-conv", "My Chat");
        _storeMock
            .Setup(x => x.CreateConversationAsync("anonymous", "My Chat", It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var request = new CreateConversationRequest { Title = "My Chat" };
        var result = await _sut.CreateConversation(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(summary);
    }

    [Fact]
    public async Task CreateConversation_UsesBodyUserId()
    {
        var summary = CreateSummary();
        _storeMock
            .Setup(x => x.CreateConversationAsync("body-user", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var request = new CreateConversationRequest { UserId = "body-user", Title = "Chat" };
        await _sut.CreateConversation(request, CancellationToken.None);

        _storeMock.Verify(
            x => x.CreateConversationAsync("body-user", It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateConversation_NullRequest_UsesAnonymous()
    {
        var summary = CreateSummary();
        _storeMock
            .Setup(x => x.CreateConversationAsync("anonymous", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        await _sut.CreateConversation(null, CancellationToken.None);

        _storeMock.Verify(
            x => x.CreateConversationAsync("anonymous", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ════════════════════════════════════════════
    //  GET CONVERSATION
    // ════════════════════════════════════════════

    #region GetConversation

    [Fact]
    public async Task GetConversation_Found_ReturnsOk()
    {
        var details = new ConversationDetails(
            CreateSummary("c1"),
            new List<ConversationMessage>());
        _storeMock
            .Setup(x => x.GetConversationAsync("c1", "anonymous", 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        var result = await _sut.GetConversation("c1");

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(details);
    }

    [Fact]
    public async Task GetConversation_NotFound_Returns404()
    {
        _storeMock
            .Setup(x => x.GetConversationAsync("c99", "anonymous", 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationDetails?)null);

        var result = await _sut.GetConversation("c99");

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    // ════════════════════════════════════════════
    //  RENAME CONVERSATION
    // ════════════════════════════════════════════

    #region RenameConversation

    [Fact]
    public async Task RenameConversation_ValidTitle_ReturnsOk()
    {
        _storeMock
            .Setup(x => x.RenameConversationAsync("c1", "anonymous", "New Title", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new RenameConversationRequest { Title = "New Title" };
        var result = await _sut.RenameConversation("c1", request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RenameConversation_EmptyTitle_ReturnsBadRequest()
    {
        var request = new RenameConversationRequest { Title = "   " };
        var result = await _sut.RenameConversation("c1", request, CancellationToken.None);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().BeEquivalentTo(new { error = "title is required." });
    }

    [Fact]
    public async Task RenameConversation_NotFound_Returns404()
    {
        _storeMock
            .Setup(x => x.RenameConversationAsync("c99", "anonymous", "Title", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new RenameConversationRequest { Title = "Title" };
        var result = await _sut.RenameConversation("c99", request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RenameConversation_UsesRequestUserId()
    {
        _storeMock
            .Setup(x => x.RenameConversationAsync("c1", "explicit-user", "T", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new RenameConversationRequest { Title = "T", UserId = "explicit-user" };
        await _sut.RenameConversation("c1", request, CancellationToken.None);

        _storeMock.Verify(
            x => x.RenameConversationAsync("c1", "explicit-user", "T", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ════════════════════════════════════════════
    //  DELETE CONVERSATION
    // ════════════════════════════════════════════

    #region DeleteConversation

    [Fact]
    public async Task DeleteConversation_Found_ReturnsOk()
    {
        _storeMock
            .Setup(x => x.DeleteConversationAsync("c1", "anonymous", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DeleteConversation("c1", null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteConversation_NotFound_Returns404()
    {
        _storeMock
            .Setup(x => x.DeleteConversationAsync("c99", "anonymous", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.DeleteConversation("c99", null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteConversation_UsesExplicitUserId()
    {
        _storeMock
            .Setup(x => x.DeleteConversationAsync("c1", "user-99", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.DeleteConversation("c1", "user-99", CancellationToken.None);

        _storeMock.Verify(
            x => x.DeleteConversationAsync("c1", "user-99", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ════════════════════════════════════════════
    //  STREAMING CHAT — PostChat (v1)
    // ════════════════════════════════════════════

    #region PostChat

    [Fact]
    public async Task PostChat_EmptyPrompt_Returns400()
    {
        // Arrange
        SetupStreamingContext();
        var request = new ChatRequestV1 { ConversationId = "c1", Prompt = "   " };

        // Act
        await _sut.PostChat(request, CancellationToken.None);

        // Assert
        _sut.HttpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task PostChat_NoConversationId_CreatesNewConversation()
    {
        // Arrange
        SetupStreamingContext();
        var summary = CreateSummary("new-conv-id");
        _storeMock
            .Setup(x => x.CreateConversationAsync("anonymous", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        _storeMock
            .Setup(x => x.AppendUserMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Agent returns a simple SSE response
        SetupAgentClient(HttpStatusCode.OK, "data: {\"type\":\"done\",\"content\":\"Hello!\"}\n\n");

        var request = new ChatRequestV1 { Prompt = "Hi" }; // no ConversationId

        // Act
        await _sut.PostChat(request, CancellationToken.None);

        // Assert — should have created a new conversation
        _storeMock.Verify(
            x => x.CreateConversationAsync("anonymous", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostChat_ValidRequest_AppendsUserMessageAndStreams()
    {
        // Arrange
        SetupStreamingContext();
        _storeMock
            .Setup(x => x.AppendUserMessageAsync("c1", "anonymous", "Hello", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storeMock
            .Setup(x => x.AppendAssistantMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupAgentClient(HttpStatusCode.OK, "data: {\"type\":\"done\",\"content\":\"Reply text\"}\n\n");

        var request = new ChatRequestV1 { ConversationId = "c1", Prompt = "Hello" };

        // Act
        await _sut.PostChat(request, CancellationToken.None);

        // Assert
        _sut.HttpContext.Response.StatusCode.Should().Be(200);
        _storeMock.Verify(
            x => x.AppendUserMessageAsync("c1", "anonymous", "Hello", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _storeMock.Verify(
            x => x.AppendAssistantMessageAsync("c1", "anonymous", "Reply text", It.IsAny<object?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostChat_UpstreamError_ReturnsUpstreamStatusCode()
    {
        // Arrange
        SetupStreamingContext();
        _storeMock
            .Setup(x => x.AppendUserMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupAgentClient(HttpStatusCode.InternalServerError, "Agent error");

        var request = new ChatRequestV1 { ConversationId = "c1", Prompt = "Hello" };

        // Act
        await _sut.PostChat(request, CancellationToken.None);

        // Assert
        _sut.HttpContext.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task PostChat_OwnershipException_Returns404()
    {
        // Arrange
        SetupStreamingContext();
        _storeMock
            .Setup(x => x.AppendUserMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConversationOwnershipException("c1"));

        var request = new ChatRequestV1 { ConversationId = "c1", Prompt = "Hello" };

        // Act
        await _sut.PostChat(request, CancellationToken.None);

        // Assert
        _sut.HttpContext.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task PostChat_AssistantPayloadAsJson_SavesPayloadAndText()
    {
        // Arrange
        SetupStreamingContext();
        _storeMock
            .Setup(x => x.AppendUserMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storeMock
            .Setup(x => x.AppendAssistantMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Agent returns a JSON payload with "text" property
        var jsonPayload = """{"text":"Here is your data","ui":{"type":"chart"}}""";
        SetupAgentClient(HttpStatusCode.OK, $"data: {{\"type\":\"done\",\"content\":{System.Text.Json.JsonSerializer.Serialize(jsonPayload)}}}\n\n");

        var request = new ChatRequestV1 { ConversationId = "c1", Prompt = "Show chart" };

        // Act
        await _sut.PostChat(request, CancellationToken.None);

        // Assert — assistant message should have been saved
        _storeMock.Verify(
            x => x.AppendAssistantMessageAsync("c1", "anonymous", It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostChat_UsesMessageFieldIfPromptEmpty()
    {
        // Arrange
        SetupStreamingContext();
        _storeMock
            .Setup(x => x.AppendUserMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupAgentClient(HttpStatusCode.OK, "data: {\"type\":\"token\",\"content\":\"hi\"}\n\n");

        var request = new ChatRequestV1 { ConversationId = "c1", Message = "From message field" };

        // Act
        await _sut.PostChat(request, CancellationToken.None);

        // Assert
        _storeMock.Verify(
            x => x.AppendUserMessageAsync("c1", "anonymous", "From message field", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ════════════════════════════════════════════
    //  STREAMING CHAT — PostLegacyChat
    // ════════════════════════════════════════════

    #region PostLegacyChat

    [Fact]
    public async Task PostLegacyChat_EmptyMessage_Returns400()
    {
        // Arrange
        SetupStreamingContext();
        var request = new LegacyChatRequest { SessionId = "s1", Message = "" };

        // Act
        await _sut.PostLegacyChat(request, CancellationToken.None);

        // Assert
        _sut.HttpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task PostLegacyChat_NoSessionId_CreatesNewConversation()
    {
        // Arrange
        SetupStreamingContext();
        var summary = CreateSummary("new-session");
        _storeMock
            .Setup(x => x.CreateConversationAsync("anonymous", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        _storeMock
            .Setup(x => x.AppendUserMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupAgentClient(HttpStatusCode.OK, "data: {\"type\":\"token\",\"content\":\"ok\"}\n\n");

        var request = new LegacyChatRequest { Message = "Hello legacy" };

        // Act
        await _sut.PostLegacyChat(request, CancellationToken.None);

        // Assert
        _storeMock.Verify(
            x => x.CreateConversationAsync("anonymous", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostLegacyChat_WithSessionId_UsesExistingConversation()
    {
        // Arrange
        SetupStreamingContext();
        _storeMock
            .Setup(x => x.AppendUserMessageAsync("existing-session", "anonymous", "Hello", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupAgentClient(HttpStatusCode.OK, "data: {\"type\":\"token\",\"content\":\"reply\"}\n\n");

        var request = new LegacyChatRequest { SessionId = "existing-session", Message = "Hello" };

        // Act
        await _sut.PostLegacyChat(request, CancellationToken.None);

        // Assert — should use existing session, not create new
        _storeMock.Verify(
            x => x.CreateConversationAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    // ════════════════════════════════════════════
    //  RESOLVE USER ID (tested through endpoints)
    // ════════════════════════════════════════════

    #region ResolveUserId

    [Fact]
    public async Task ResolveUserId_ExplicitUserId_HasHighestPriority()
    {
        SetHeaderUserId("header-user");
        SetQueryUserId("query-user");

        var summary = CreateSummary();
        _storeMock
            .Setup(x => x.CreateConversationAsync("body-user", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var request = new CreateConversationRequest { UserId = "body-user" };
        await _sut.CreateConversation(request, CancellationToken.None);

        _storeMock.Verify(
            x => x.CreateConversationAsync("body-user", It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolveUserId_HeaderFallback_WhenNoExplicitId()
    {
        SetHeaderUserId("header-user");

        _storeMock
            .Setup(x => x.ListConversationsAsync("header-user", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversationSummary>());

        await _sut.ListConversations();

        _storeMock.Verify(
            x => x.ListConversationsAsync("header-user", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolveUserId_QueryFallback_WhenNoHeaderOrExplicit()
    {
        SetQueryUserId("query-user");

        _storeMock
            .Setup(x => x.ListConversationsAsync("query-user", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversationSummary>());

        await _sut.ListConversations();

        _storeMock.Verify(
            x => x.ListConversationsAsync("query-user", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolveUserId_NoIdAnywhere_ReturnsAnonymous()
    {
        _storeMock
            .Setup(x => x.ListConversationsAsync("anonymous", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversationSummary>());

        await _sut.ListConversations();

        _storeMock.Verify(
            x => x.ListConversationsAsync("anonymous", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ════════════════════════════════════════════
    //  MODEL TESTS
    // ════════════════════════════════════════════

    #region Models

    [Fact]
    public void ChatRequestV1_DefaultValues()
    {
        var request = new ChatRequestV1();

        request.ConversationId.Should().BeNull();
        request.UserId.Should().BeNull();
        request.Prompt.Should().BeNull();
        request.Message.Should().BeNull();
    }

    [Fact]
    public void LegacyChatRequest_DefaultValues()
    {
        var request = new LegacyChatRequest();

        request.SessionId.Should().BeEmpty();
        request.Message.Should().BeEmpty();
    }

    [Fact]
    public void CreateConversationRequest_DefaultValues()
    {
        var request = new CreateConversationRequest();

        request.Title.Should().BeNull();
        request.UserId.Should().BeNull();
    }

    [Fact]
    public void RenameConversationRequest_DefaultValues()
    {
        var request = new RenameConversationRequest();

        request.Title.Should().BeEmpty();
        request.UserId.Should().BeNull();
    }

    [Fact]
    public void AgentChatRequest_SetsFields()
    {
        var request = new AgentChatRequest("session-123", "Hello world");

        request.SessionId.Should().Be("session-123");
        request.Message.Should().Be("Hello world");
    }

    #endregion
}

/// <summary>
/// Mock HTTP handler for upstream agent service calls.
/// </summary>
public class MockAgentHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _body;

    public MockAgentHttpHandler(HttpStatusCode statusCode, string body)
    {
        _statusCode = statusCode;
        _body = body;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_body, Encoding.UTF8,
                _statusCode == HttpStatusCode.OK ? "text/event-stream" : "application/json")
        };
        return Task.FromResult(response);
    }
}