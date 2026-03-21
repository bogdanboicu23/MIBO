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
public class ConversationCrudTests : IAsyncLifetime
{
    private readonly SharedContainersFixture _containers;
    private WireMockServer _wireMock = null!;
    private ConversationServiceFactory _factory = null!;
    private HttpClient _client = null!;

    private const string TestUserId = "e2e-crud-user";

    public ConversationCrudTests(SharedContainersFixture containers)
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

    private async Task<string> CreateConversationAsync(string title = "Test Conversation")
    {
        var response = await _client.PostAsJsonAsync("/api/v1/conversations", new
        {
            title,
            userId = TestUserId
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("conversationId").GetString()!;
    }

    // ── Create ──

    [Fact]
    public async Task CreateConversation_WithTitle_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/conversations", new
        {
            title = "Test Conversation",
            userId = TestUserId
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("conversationId").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("title").GetString().Should().Be("Test Conversation");
        body.GetProperty("userId").GetString().Should().Be(TestUserId);
    }

    [Fact]
    public async Task CreateConversation_WithoutTitle_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/conversations", new
        {
            userId = TestUserId
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("conversationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    // ── List ──

    [Fact]
    public async Task ListConversations_ReturnsUserConversations()
    {
        await CreateConversationAsync("List Test");

        var response = await _client.GetAsync("/api/v1/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        body.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ListConversations_WithPagination_RespectsSkipLimit()
    {
        await CreateConversationAsync("Page 1");
        await CreateConversationAsync("Page 2");
        await CreateConversationAsync("Page 3");

        var response = await _client.GetAsync("/api/v1/conversations?skip=0&limit=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task ListConversations_WithSkip_SkipsResults()
    {
        await CreateConversationAsync("Skip A");
        await CreateConversationAsync("Skip B");

        var allResponse = await _client.GetAsync("/api/v1/conversations?limit=50");
        var allBody = await allResponse.Content.ReadFromJsonAsync<JsonElement>();
        var totalCount = allBody.GetArrayLength();

        var skippedResponse = await _client.GetAsync("/api/v1/conversations?skip=1&limit=50");
        var skippedBody = await skippedResponse.Content.ReadFromJsonAsync<JsonElement>();

        skippedBody.GetArrayLength().Should().BeLessThanOrEqualTo(totalCount);
    }

    // ── Get ──

    [Fact]
    public async Task GetConversation_ExistingId_ReturnsDetails()
    {
        var conversationId = await CreateConversationAsync("Get Test");

        var response = await _client.GetAsync($"/api/v1/conversations/{conversationId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("conversation").GetProperty("conversationId").GetString().Should().Be(conversationId);
        body.GetProperty("messages").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetConversation_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/api/v1/conversations/000000000000000000000000");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConversation_WrongUser_Returns404()
    {
        var conversationId = await CreateConversationAsync("Owned by crud-user");

        // Create a new client with a different user
        var otherClient = _factory.CreateClient();
        otherClient.DefaultRequestHeaders.Add("X-User-Id", "other-user");

        var response = await otherClient.GetAsync($"/api/v1/conversations/{conversationId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        otherClient.Dispose();
    }

    [Fact]
    public async Task GetConversation_WithMessagesLimit_Works()
    {
        var conversationId = await CreateConversationAsync("Msg Limit Test");

        var response = await _client.GetAsync($"/api/v1/conversations/{conversationId}?messagesLimit=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Rename ──

    [Fact]
    public async Task RenameConversation_WithValidTitle_ReturnsOk()
    {
        var conversationId = await CreateConversationAsync("Original Title");

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/conversations/{conversationId}")
        {
            Content = JsonContent.Create(new { title = "New Title", userId = TestUserId })
        };
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("ok").GetBoolean().Should().BeTrue();

        // Verify the rename stuck
        var getResponse = await _client.GetAsync($"/api/v1/conversations/{conversationId}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        getBody.GetProperty("conversation").GetProperty("title").GetString().Should().Be("New Title");
    }

    [Fact]
    public async Task RenameConversation_EmptyTitle_Returns400()
    {
        var conversationId = await CreateConversationAsync("To Rename");

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/conversations/{conversationId}")
        {
            Content = JsonContent.Create(new { title = "", userId = TestUserId })
        };
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RenameConversation_WhitespaceTitle_Returns400()
    {
        var conversationId = await CreateConversationAsync("To Rename WS");

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/conversations/{conversationId}")
        {
            Content = JsonContent.Create(new { title = "   ", userId = TestUserId })
        };
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RenameConversation_NonExistent_Returns404()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/conversations/000000000000000000000000")
        {
            Content = JsonContent.Create(new { title = "New", userId = TestUserId })
        };
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Delete ──

    [Fact]
    public async Task DeleteConversation_ThenGet_Returns404()
    {
        var conversationId = await CreateConversationAsync("To Delete");

        var deleteResponse = await _client.DeleteAsync(
            $"/api/v1/conversations/{conversationId}?userId={TestUserId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/v1/conversations/{conversationId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteConversation_NonExistent_Returns404()
    {
        var response = await _client.DeleteAsync(
            $"/api/v1/conversations/000000000000000000000000?userId={TestUserId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

}
