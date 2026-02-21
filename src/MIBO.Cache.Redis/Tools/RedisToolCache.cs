using MIBO.ConversationService.Services.Redis;
using StackExchange.Redis;

namespace MIBO.Cache.Redis.Tools;

public sealed class RedisToolCache : IToolCache
{
    private readonly IDatabase _db;

    public RedisToolCache(IConnectionMultiplexer mux)
        => _db = mux.GetDatabase();

    public async Task<string?> GetAsync(string key, CancellationToken ct)
    {
        var v = await _db.StringGetAsync(key);
        return v.HasValue ? v.ToString() : null;
    }

    public Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct)
        => _db.StringSetAsync(key, value, ttl);
}