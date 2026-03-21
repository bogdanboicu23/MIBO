using System.Net;
using System.Text;
using FluentAssertions;
using MIBO.IdentityService.Models;

namespace MIBO.IdentityService.Tests.Unit.Models;

public class TurnstileVerifierTests
{
    // ──── Helper: create an HttpClient that returns a controlled response ────

    private static HttpClient CreateMockClient(HttpStatusCode statusCode, string jsonBody)
    {
        var handler = new MockHttpMessageHandler(statusCode, jsonBody);
        return new HttpClient(handler);
    }

    // ════════════════════════════════════════════
    //  VerifyAsync
    // ════════════════════════════════════════════

    [Fact]
    public async Task VerifyAsync_SuccessResponse_ReturnsTrue()
    {
        // Arrange
        var client = CreateMockClient(HttpStatusCode.OK,
            """{"success": true, "error-codes": []}""");

        // Act
        var (ok, errors) = await TurnstileVerifier.VerifyAsync(
            "valid-token", "secret", "127.0.0.1", client, CancellationToken.None);

        // Assert
        ok.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifyAsync_FailureResponse_ReturnsFalseWithErrors()
    {
        // Arrange
        var client = CreateMockClient(HttpStatusCode.OK,
            """{"success": false, "error-codes": ["invalid-input-response"]}""");

        // Act
        var (ok, errors) = await TurnstileVerifier.VerifyAsync(
            "bad-token", "secret", null, client, CancellationToken.None);

        // Assert
        ok.Should().BeFalse();
        errors.Should().Contain("invalid-input-response");
    }

    [Fact]
    public async Task VerifyAsync_HttpError_ReturnsFalse()
    {
        // Arrange
        var client = CreateMockClient(HttpStatusCode.InternalServerError, "");

        // Act
        var (ok, errors) = await TurnstileVerifier.VerifyAsync(
            "token", "secret", null, client, CancellationToken.None);

        // Assert
        ok.Should().BeFalse();
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task VerifyAsync_InvalidJsonResponse_ReturnsFalse()
    {
        // Arrange — server returns garbage
        var client = CreateMockClient(HttpStatusCode.OK, "not-json");

        // Act
        var (ok, errors) = await TurnstileVerifier.VerifyAsync(
            "token", "secret", null, client, CancellationToken.None);

        // Assert
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyAsync_NullResponse_ReturnsFalse()
    {
        // Arrange — server returns JSON "null"
        var client = CreateMockClient(HttpStatusCode.OK, "null");

        // Act
        var (ok, errors) = await TurnstileVerifier.VerifyAsync(
            "token", "secret", null, client, CancellationToken.None);

        // Assert
        ok.Should().BeFalse();
        errors.Should().Contain("Invalid response from Turnstile API");
    }

    [Fact]
    public async Task VerifyAsync_Timeout_ReturnsFalseWithTimeoutError()
    {
        // Arrange — handler throws TaskCanceledException (simulates timeout)
        var handler = new TimeoutHttpMessageHandler();
        var client = new HttpClient(handler);

        // Act
        var (ok, errors) = await TurnstileVerifier.VerifyAsync(
            "token", "secret", null, client, CancellationToken.None);

        // Assert
        ok.Should().BeFalse();
        errors.Should().Contain("Request timeout");
    }

    [Fact]
    public async Task VerifyAsync_NetworkError_ReturnsFalseWithNetworkError()
    {
        // Arrange — handler throws HttpRequestException
        var handler = new NetworkErrorHttpMessageHandler();
        var client = new HttpClient(handler);

        // Act
        var (ok, errors) = await TurnstileVerifier.VerifyAsync(
            "token", "secret", null, client, CancellationToken.None);

        // Assert
        ok.Should().BeFalse();
        errors.Should().ContainMatch("*Network error*");
    }

    [Fact]
    public async Task VerifyAsync_NullIp_SendsEmptyString()
    {
        // Arrange — ip is null, should not throw
        var client = CreateMockClient(HttpStatusCode.OK,
            """{"success": true, "error-codes": []}""");

        // Act
        var (ok, _) = await TurnstileVerifier.VerifyAsync(
            "token", "secret", null, client, CancellationToken.None);

        // Assert — should work fine with null IP
        ok.Should().BeTrue();
    }
}

// ──── Test helpers ────

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _body;

    public MockHttpMessageHandler(HttpStatusCode statusCode, string body)
    {
        _statusCode = statusCode;
        _body = body;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_body, Encoding.UTF8, "application/json")
        });
    }
}

public class TimeoutHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new TaskCanceledException("The request timed out.");
    }
}

public class NetworkErrorHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new HttpRequestException("Connection refused");
    }
}
