using System.Text.Json;

namespace MIBO.ActionService.ExternalServices.Spotify;

public interface ISpotifyApiClient
{
    Task<JsonElement> SearchAsync(string accessToken, string query, string type, int limit, CancellationToken ct);
    Task<JsonElement> GetCurrentlyPlayingAsync(string accessToken, CancellationToken ct);
    Task<JsonElement> GetUserPlaylistsAsync(string accessToken, int limit, CancellationToken ct);
    Task<JsonElement> GetUserTopItemsAsync(string accessToken, string type, string timeRange, int limit, CancellationToken ct);
}
