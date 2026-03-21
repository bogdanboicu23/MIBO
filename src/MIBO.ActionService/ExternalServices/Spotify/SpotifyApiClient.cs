using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MIBO.ActionService.ExternalServices.Spotify;

public sealed class SpotifyApiClient(HttpClient httpClient) : ISpotifyApiClient
{
    public async Task<JsonElement> SearchAsync(string accessToken, string query, string type, int limit, CancellationToken ct)
    {
        SetAuth(accessToken);
        var url = $"search?q={Uri.EscapeDataString(query)}&type={Uri.EscapeDataString(type)}&limit={limit}";
        return await GetJsonAsync(url, ct);
    }

    public async Task<JsonElement> GetCurrentlyPlayingAsync(string accessToken, CancellationToken ct)
    {
        SetAuth(accessToken);
        return await GetJsonAsync("me/player/currently-playing", ct);
    }

    public async Task<JsonElement> GetUserPlaylistsAsync(string accessToken, int limit, CancellationToken ct)
    {
        SetAuth(accessToken);
        return await GetJsonAsync($"me/playlists?limit={limit}", ct);
    }

    public async Task<JsonElement> GetUserTopItemsAsync(string accessToken, string type, string timeRange, int limit, CancellationToken ct)
    {
        SetAuth(accessToken);
        return await GetJsonAsync($"me/top/{Uri.EscapeDataString(type)}?time_range={Uri.EscapeDataString(timeRange)}&limit={limit}", ct);
    }

    private void SetAuth(string accessToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<JsonElement> GetJsonAsync(string url, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(ct);
    }
}
