using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MIBO.Cache.Redis.Spotify;

public sealed class SpotifyTokenRefresher(
    ISpotifyTokenStore tokenStore,
    IHttpClientFactory httpClientFactory,
    IOptions<SpotifyClientOptions> options,
    ILogger<SpotifyTokenRefresher> logger) : ISpotifyTokenRefresher
{
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(60);

    public async Task<string?> GetValidAccessTokenAsync(string userId, CancellationToken ct = default)
    {
        var tokens = await tokenStore.GetTokensAsync(userId, ct);
        if (tokens is null)
        {
            logger.LogWarning("Spotify: no tokens found in Redis for user {UserId}.", userId);
            return null;
        }

        if (tokens.ExpiresAtUtc > DateTime.UtcNow.Add(ExpiryBuffer))
        {
            logger.LogDebug("Spotify: using cached access token for user {UserId} (expires {ExpiresAt}).", userId, tokens.ExpiresAtUtc);
            return tokens.AccessToken;
        }

        logger.LogInformation("Spotify: access token expired for user {UserId} (expired {ExpiresAt}), refreshing.", userId, tokens.ExpiresAtUtc);
        return await RefreshAsync(userId, tokens.RefreshToken, ct);
    }

    private async Task<string?> RefreshAsync(string userId, string refreshToken, CancellationToken ct)
    {
        var opts = options.Value;
        var client = httpClientFactory.CreateClient("spotify");

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        });

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{opts.ClientId}:{opts.ClientSecret}"));
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
        {
            Content = body,
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Spotify: token refresh failed for user {UserId}. Status {StatusCode}, body: {ErrorBody}",
                userId, (int)response.StatusCode, errorBody);
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        var accessToken = json.GetProperty("access_token").GetString()!;
        var expiresIn = json.GetProperty("expires_in").GetInt32();
        var newRefreshToken = json.TryGetProperty("refresh_token", out var rt)
            ? rt.GetString() ?? refreshToken
            : refreshToken;

        await tokenStore.SaveTokensAsync(userId, accessToken, newRefreshToken, expiresIn, ct);
        logger.LogInformation("Spotify: token refreshed for user {UserId}, expires in {ExpiresIn}s.", userId, expiresIn);
        return accessToken;
    }
}
