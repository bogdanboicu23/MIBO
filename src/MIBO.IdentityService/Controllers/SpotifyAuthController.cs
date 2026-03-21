using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MIBO.Cache.Redis.Spotify;
using MIBO.IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MIBO.IdentityService.Controllers;

[ApiController]
[Route("api/auth/spotify")]
public sealed class SpotifyAuthController(
    IOptions<SpotifyOptions> spotifyOptions,
    ISpotifyTokenStore tokenStore,
    IHttpClientFactory httpClientFactory) : ControllerBase
{
    private readonly SpotifyOptions _options = spotifyOptions.Value;

    [Authorize]
    [HttpGet("authorize")]
    public IActionResult Authorize()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        var query = string.Join("&",
            $"response_type=code",
            $"client_id={Uri.EscapeDataString(_options.ClientId)}",
            $"scope={Uri.EscapeDataString(_options.Scopes)}",
            $"redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}",
            $"state={Uri.EscapeDataString(userId)}");

        var url = $"https://accounts.spotify.com/authorize?{query}";
        return Ok(new { url });
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken ct)
    {
        var userId = state;
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            return BadRequest(new { error = "Invalid callback parameters." });

        var client = httpClientFactory.CreateClient("spotify");

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri,
        });

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
        {
            Content = body,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return BadRequest(new { error = "Token exchange failed.", details = errorBody });
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var accessToken = json.GetProperty("access_token").GetString()!;
        var refreshToken = json.GetProperty("refresh_token").GetString()!;
        var expiresIn = json.GetProperty("expires_in").GetInt32();

        await tokenStore.SaveTokensAsync(userId, accessToken, refreshToken, expiresIn, ct);

        var separator = _options.FrontendRedirectUrl.Contains('?') ? '&' : '?';
        return Redirect($"{_options.FrontendRedirectUrl}{separator}spotify=connected");
    }

    [Authorize]
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var connected = await tokenStore.HasTokensAsync(userId, ct);
        return Ok(new { connected });
    }

    [Authorize]
    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        await tokenStore.RemoveTokensAsync(userId, ct);
        return Ok(new { disconnected = true });
    }
}
