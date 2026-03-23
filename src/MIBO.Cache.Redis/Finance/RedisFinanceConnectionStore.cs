using StackExchange.Redis;

namespace MIBO.Cache.Redis.Finance;

public sealed class RedisFinanceConnectionStore(IConnectionMultiplexer mux) : IFinanceConnectionStore
{
    private readonly IDatabase _db = mux.GetDatabase();

    private static string Key(string userId) => $"finance:connected:{userId}";

    public async Task SetConnectedAsync(string userId, CancellationToken ct = default)
    {
        await _db.StringSetAsync(Key(userId), "1");
    }

    public async Task SetDisconnectedAsync(string userId, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync(Key(userId));
    }

    public async Task<bool> IsConnectedAsync(string userId, CancellationToken ct = default)
    {
        return await _db.KeyExistsAsync(Key(userId));
    }
}
