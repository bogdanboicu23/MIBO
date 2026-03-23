namespace MIBO.Cache.Redis.Finance;

public interface IFinanceConnectionStore
{
    Task SetConnectedAsync(string userId, CancellationToken ct = default);
    Task SetDisconnectedAsync(string userId, CancellationToken ct = default);
    Task<bool> IsConnectedAsync(string userId, CancellationToken ct = default);
}
