using System.Text.Json;
using StackExchange.Redis;

namespace MIBO.Cache.Redis.Spotify;

public sealed class RedisSpotifyTokenStore(IConnectionMultiplexer mux) : ISpotifyTokenStore
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(30);
    private readonly IDatabase _db = mux.GetDatabase();

    private static string Key(string userId) => $"spotify:tokens:{userId}";

    public async Task SaveTokensAsync(string userId, string accessToken, string refreshToken, int expiresInSeconds, CancellationToken ct = default)
    {
        var tokens = new SpotifyTokens(accessToken, refreshToken, DateTime.UtcNow.AddSeconds(expiresInSeconds));
        var json = JsonSerializer.Serialize(tokens);
        await _db.StringSetAsync(Key(userId), json, DefaultTtl);
    }

    public async Task<SpotifyTokens?> GetTokensAsync(string userId, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(Key(userId));
        if (!value.HasValue)
            return null;

        return JsonSerializer.Deserialize<SpotifyTokens>(value.ToString());
    }

    public async Task RemoveTokensAsync(string userId, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync(Key(userId));
    }

    public async Task<bool> HasTokensAsync(string userId, CancellationToken ct = default)
    {
        return await _db.KeyExistsAsync(Key(userId));
    }
}
