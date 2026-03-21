extern alias ConversationService;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MIBO.E2ETests.Fixtures;
using MIBO.E2ETests.Helpers;
using WireMock.Server;

namespace MIBO.E2ETests.ConversationService;

[Collection("E2E")]
public class ChatStreamingTests : IAsyncLifetime
{
    private readonly SharedContainersFixture _containers;
    private WireMockServer _wireMock = null!;
    private ConversationServiceFactory _factory = null!;
    private HttpClient _client = null!;

    private const string TestUserId = "e2e-chat-user";

    public ChatStreamingTests(SharedContainersFixture containers)
    {
        _containers = containers;
    }

    public Task InitializeAsync()
    {
        _wireMock = WireMockServer.Start();
        _factory = new ConversationServiceFactory(
            _containers.MongoConnectionString,
            _wireMock.Url!,
            _wireMock.Url!);
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-User-Id", TestUserId);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        _wireMock.Dispose();
    }

    // ── V1 Chat ──

    [Fact]
    public async Task Chat_NewConversation_ReturnsSseStreamWithSessionEvent()
    {
        WireMockSetup.StubAgentChatSse(_wireMock);

        var response = await _client.PostAsJsonAsync("/api/v1/chat", new
        {
            prompt = "Hello",
            userId = TestUserId
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");
        response.Headers.Should().ContainKey("X-Conversation-Id");

        var conversationId = response.Headers.GetValues("X-Conversation-Id").First();
        conversationId.Should().NotBeNullOrWhiteSpace();

        var events = await SseStreamReader.ReadEventsAsync(response);
        events.Should().Contain(e => e.Type == "session");
        events.Should().Contain(e => e.Type == "token" || e.Type == "done");
    }

    [Fact]
    public async Task Chat_ExistingConversation_NoSessionEvent()
    {
        WireMockSetup.StubAgentChatSse(_wireMock);

        // First chat creates a conversation
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/chat", new
        {
            prompt = "Hello",
            userId = TestUserId
        });
        var conversationId = firstResponse.Headers.GetValues("X-Conversation-Id").First();
        await SseStreamReader.ReadEventsAsync(firstResponse);

        // Second chat on same conversation
        _wireMock.Reset();
        WireMockSetup.StubAgentChatSse(_wireMock);

        var secondResponse = await _client.PostAsJsonAsync("/api/v1/chat", new
        {
            conversationId,
            prompt = "Follow up",
            userId = TestUserId
        });

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await SseStreamReader.ReadEventsAsync(secondResponse);
        events.Should().NotContain(e => e.Type == "session");
    }

    [Fact]
    public async Task Chat_CreatesConversationAndStoresMessages()
    {
        WireMockSetup.StubAgentChatSse(_wireMock, "Agent reply");

        var chatResponse = await _client.PostAsJsonAsync("/api/v1/chat", new
        {
            prompt = "Store this message",
            userId = TestUserId
        });
        var conversationId = chatResponse.Headers.GetValues("X-Conversation-Id").First();
        await SseStreamReader.ReadEventsAsync(chatResponse);

        // Verify conversation exists and has messages
        var getResponse = await _client.GetAsync($"/api/v1/conversations/{conversationId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("messages").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Chat_MultipleMessages_AllStored()
    {
        WireMockSetup.StubAgentChatSse(_wireMock, "Reply 1");

        var resp1 = await _client.PostAsJsonAsync("/api/v1/chat", new
        {
            prompt = "Message 1",
            userId = TestUserId
        });
        var conversationId = resp1.Headers.GetValues("X-Conversation-Id").First();
        await SseStreamReader.ReadEventsAsync(resp1);

        _wireMock.Reset();
        WireMockSetup.StubAgentChatSse(_wireMock, "Reply 2");

        var resp2 = await _client.PostAsJsonAsync("/api/v1/chat", new
        {
            conversationId,
            prompt = "Message 2",
            userId = TestUserId
        });
        await SseStreamReader.ReadEventsAsync(resp2);

        var getResponse = await _client.GetAsync($"/api/v1/conversations/{conversationId}");
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        // Should have at least 2 user messages + 2 assistant messages
        body.GetProperty("messages").GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Chat_EmptyPrompt_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/chat", new
        {
            prompt = "",
            userId = TestUserId
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Chat_WhitespacePrompt_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/chat", new
        {
            prompt = "   ",
            userId = TestUserId
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Chat_NullPrompt_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/chat", new
        {
            userId = TestUserId
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Chat_WithMessageField_AcceptsAlternateField()
    {
        WireMockSetup.StubAgentChatSse(_wireMock);

        var response = await _client.PostAsJsonAsync("/api/v1/chat", new
        {
            message = "Using message field",
            userId = TestUserId
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Legacy Chat ──

    [Fact]
    public async Task LegacyChat_NewSession_ReturnsStream()
    {
        WireMockSetup.StubAgentChatSse(_wireMock);

        var response = await _client.PostAsJsonAsync("/api/chat", new
        {
            message = "Hello legacy",
            session_id = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");

        var events = await SseStreamReader.ReadEventsAsync(response);
        events.Should().Contain(e => e.Type == "session");
    }

    [Fact]
    public async Task LegacyChat_ExistingSession_NoSessionEvent()
    {
        WireMockSetup.StubAgentChatSse(_wireMock);

        // First call creates a session
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/chat", new
        {
            prompt = "Init",
            userId = TestUserId
        });
        var conversationId = firstResponse.Headers.GetValues("X-Conversation-Id").First();
        await SseStreamReader.ReadEventsAsync(firstResponse);

        _wireMock.Reset();
        WireMockSetup.StubAgentChatSse(_wireMock);

        // Legacy call with existing session
        var response = await _client.PostAsJsonAsync("/api/chat", new
        {
            message = "Legacy follow up",
            session_id = conversationId
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await SseStreamReader.ReadEventsAsync(response);
        events.Should().NotContain(e => e.Type == "session");
    }

    // ── Error propagation ──

    [Fact]
    public async Task Chat_AgentReturns500_PropagatesError()
    {
        WireMockSetup.StubAgentChatError(_wireMock, 500);

        var response = await _client.PostAsJsonAsync("/api/v1/chat", new
        {
            prompt = "This will fail",
            userId = TestUserId
        });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Chat_AgentReturns502_PropagatesError()
    {
        WireMockSetup.StubAgentChatError(_wireMock, 502);

        var response = await _client.PostAsJsonAsync("/api/v1/chat", new
        {
            prompt = "Gateway error",
            userId = TestUserId
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    // ── User resolution ──

    [Fact]
    public async Task Chat_OwnershipViolation_Returns404()
    {
        WireMockSetup.StubAgentChatSse(_wireMock);

        // Create a conversation as TestUserId
        var createResponse = await _client.PostAsJsonAsync("/api/v1/chat", new
        {
            prompt = "Create session",
            userId = TestUserId
        });
        var conversationId = createResponse.Headers.GetValues("X-Conversation-Id").First();
        await SseStreamReader.ReadEventsAsync(createResponse);

        _wireMock.Reset();
        WireMockSetup.StubAgentChatSse(_wireMock);

        // Try to chat on that conversation as a different user
        var otherClient = _factory.CreateClient();
        otherClient.DefaultRequestHeaders.Add("X-User-Id", "different-user");

        var response = await otherClient.PostAsJsonAsync("/api/v1/chat", new
        {
            conversationId,
            prompt = "Hijack attempt",
            userId = "different-user"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        otherClient.Dispose();
    }

    [Fact]
    public async Task Chat_WithQueryUserId_UsesQueryParam()
    {
        WireMockSetup.StubAgentChatSse(_wireMock);

        // Create a client without X-User-Id header
        var rawClient = _factory.CreateClient();

        var response = await rawClient.PostAsJsonAsync("/api/v1/chat", new
        {
            prompt = "Hello",
            userId = "explicit-user"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        rawClient.Dispose();
    }

    [Fact]
    public async Task Chat_WithoutAnyUserId_UsesAnonymous()
    {
        WireMockSetup.StubAgentChatSse(_wireMock);

        var rawClient = _factory.CreateClient();

        var response = await rawClient.PostAsJsonAsync("/api/v1/chat", new
        {
            prompt = "Anonymous hello"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        rawClient.Dispose();
    }
}
