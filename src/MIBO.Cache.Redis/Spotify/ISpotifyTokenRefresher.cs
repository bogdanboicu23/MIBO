namespace MIBO.Cache.Redis.Spotify;

public interface ISpotifyTokenRefresher
{
    Task<string?> GetValidAccessTokenAsync(string userId, CancellationToken ct = default);
}
