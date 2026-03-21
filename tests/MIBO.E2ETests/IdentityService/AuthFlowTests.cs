extern alias IdentityService;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MIBO.E2ETests.Fixtures;

namespace MIBO.E2ETests.IdentityService;

[Collection("E2E")]
public class AuthFlowTests : IAsyncLifetime
{
    private readonly SharedContainersFixture _containers;
    private IdentityServiceFactory _factory = null!;
    private HttpClient _client = null!;

    public AuthFlowTests(SharedContainersFixture containers)
    {
        _containers = containers;
    }

    public Task InitializeAsync()
    {
        _factory = new IdentityServiceFactory(_containers.PostgresConnectionString);
        _client = new HttpClient(_factory.Server.CreateHandler())
        {
            BaseAddress = new Uri("http://localhost")
        };
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<string> RegisterAndLoginAsync(string? email = null, string password = "Test1234")
    {
        email ??= $"user-{Guid.NewGuid():N}@example.com";
        var username = $"user{Guid.NewGuid():N}";

        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            username,
            firstName = "Test",
            lastName = "User"
        });
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("jwtToken").GetString()!;
    }

    private HttpRequestMessage AuthenticatedGet(string url, string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    // ── Registration ──

    [Fact]
    public async Task Register_WithValidData_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"reg-valid-{Guid.NewGuid():N}@example.com",
            password = "Test1234",
            username = $"user{Guid.NewGuid():N}",
            firstName = "Test",
            lastName = "User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Contain("registered successfully");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Test1234",
            username = $"user{Guid.NewGuid():N}",
            firstName = "Test",
            lastName = "User"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Test1234",
            username = $"user{Guid.NewGuid():N}",
            firstName = "Other",
            lastName = "User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithoutFirstLastName_Succeeds()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"nofn-{Guid.NewGuid():N}@example.com",
            password = "Test1234",
            username = $"user{Guid.NewGuid():N}"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_DuplicateUsername_Returns400()
    {
        var username = $"dupuser{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"u1-{Guid.NewGuid():N}@example.com",
            password = "Test1234",
            username
        });

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"u2-{Guid.NewGuid():N}@example.com",
            password = "Test1234",
            username
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Login ──

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwt()
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Test1234",
            username = $"user{Guid.NewGuid():N}"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Test1234" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("jwtToken").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns400()
    {
        var email = $"login-wrong-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Test1234",
            username = $"user{Guid.NewGuid():N}"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "doesnotexist@example.com",
            password = "Test1234"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_Twice_ReturnsFreshTokenEachTime()
    {
        var email = $"login2x-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Test1234",
            username = $"user{Guid.NewGuid():N}"
        });

        var resp1 = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Test1234" });
        var body1 = await resp1.Content.ReadFromJsonAsync<JsonElement>();
        var token1 = body1.GetProperty("jwtToken").GetString();

        var resp2 = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Test1234" });
        var body2 = await resp2.Content.ReadFromJsonAsync<JsonElement>();
        var token2 = body2.GetProperty("jwtToken").GetString();

        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        // Tokens should differ (different JTI)
        token1.Should().NotBe(token2);
    }

    // ── GET /api/auth/me ──

    [Fact]
    public async Task Me_WithValidJwt_ReturnsUserInfo()
    {
        var email = $"me-{Guid.NewGuid():N}@example.com";
        var token = await RegisterAndLoginAsync(email);

        var response = await _client.SendAsync(AuthenticatedGet("/api/auth/me", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("user").GetProperty("email").GetString().Should().Be(email);
        body.GetProperty("roles").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithInvalidToken_Returns401()
    {
        var response = await _client.SendAsync(AuthenticatedGet("/api/auth/me", "invalid.token.here"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Refresh Token ──

    [Fact]
    public async Task RefreshToken_WithoutCookie_ReturnsUnauthorized()
    {
        var token = await RegisterAndLoginAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", new { jwtToken = token });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Test endpoints ──

    [Fact]
    public async Task Test_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/auth/test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Contain("running");
        body.GetProperty("timestamp").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task TestAuth_WithToken_ReturnsOk()
    {
        var token = await RegisterAndLoginAsync();

        var response = await _client.SendAsync(AuthenticatedGet("/api/auth/test-auth", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Contain("authenticated");
        body.GetProperty("claims").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TestAuth_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/test-auth");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Health check ──

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/api/auth/test");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
