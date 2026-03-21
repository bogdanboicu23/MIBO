using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MIBO.Cache.Redis.Spotify;

public sealed class SpotifyTokenRefresher(
    ISpotifyTokenStore tokenStore,
    IHttpClientFactory httpClientFactory,
    IOptions<SpotifyClientOptions> options) : ISpotifyTokenRefresher
{
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(60);

    public async Task<string?> GetValidAccessTokenAsync(string userId, CancellationToken ct = default)
    {
        var tokens = await tokenStore.GetTokensAsync(userId, ct);
        if (tokens is null)
            return null;

        if (tokens.ExpiresAtUtc > DateTime.UtcNow.Add(ExpiryBuffer))
            return tokens.AccessToken;

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
            return null;

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        var accessToken = json.GetProperty("access_token").GetString()!;
        var expiresIn = json.GetProperty("expires_in").GetInt32();
        var newRefreshToken = json.TryGetProperty("refresh_token", out var rt)
            ? rt.GetString() ?? refreshToken
            : refreshToken;

        await tokenStore.SaveTokensAsync(userId, accessToken, newRefreshToken, expiresIn, ct);
        return accessToken;
    }
}
