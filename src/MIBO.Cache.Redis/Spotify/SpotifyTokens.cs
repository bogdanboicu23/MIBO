namespace MIBO.Cache.Redis.Spotify;

public sealed record SpotifyTokens(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
