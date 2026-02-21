namespace MIBO.ConversationService.Services.Redis;

public interface IToolCache
{
    Task<string?> GetAsync(string key, CancellationToken ct);
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct);
}