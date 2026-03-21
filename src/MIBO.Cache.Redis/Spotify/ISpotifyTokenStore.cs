namespace MIBO.Cache.Redis.Spotify;

public interface ISpotifyTokenStore
{
    Task SaveTokensAsync(string userId, string accessToken, string refreshToken, int expiresInSeconds, CancellationToken ct = default);
    Task<SpotifyTokens?> GetTokensAsync(string userId, CancellationToken ct = default);
    Task RemoveTokensAsync(string userId, CancellationToken ct = default);
    Task<bool> HasTokensAsync(string userId, CancellationToken ct = default);
}
