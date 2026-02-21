using System.Collections.Concurrent;

namespace MIBO.ConversationService.Helper;

public sealed class SingleFlight
{
    private readonly ConcurrentDictionary<string, Lazy<Task<object>>> _inflight = new();

    public async Task<T> DoAsync<T>(string key, Func<Task<T>> factory)
    {
        var lazy = _inflight.GetOrAdd(key, _ => new Lazy<Task<object>>(async () => (object)await factory()));
        try
        {
            var obj = await lazy.Value.ConfigureAwait(false);
            return (T)obj;
        }
        finally
        {
            _inflight.TryRemove(key, out _);
        }
    }
}
