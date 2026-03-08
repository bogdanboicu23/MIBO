using System.Text.Json;
using MIBO.ConversationService.DTOs.Actions;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Actions.RoutingSpecProvider;

public sealed class ActionRoutingOptions
{
    public string Source { get; set; } = default!; // /config/action-routing.json
}

public sealed class JsonFileActionRoutingSpecProvider : IActionRoutingSpecProvider
{
    private readonly ActionRoutingOptions _opt;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ActionRoutingSpec? _cache;
    private DateTime _cacheFileUtc;

    public JsonFileActionRoutingSpecProvider(IOptions<ActionRoutingOptions> opt) => _opt = opt.Value;

    public async Task<ActionRoutingSpec> GetAsync(CancellationToken ct)
    {
        var source = _opt.Source;
        if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
            return new ActionRoutingSpec();

        var fileUtc = File.GetLastWriteTimeUtc(source);
        if (_cache is not null && fileUtc <= _cacheFileUtc)
            return _cache;

        await _gate.WaitAsync(ct);
        try
        {
            fileUtc = File.GetLastWriteTimeUtc(source);
            if (_cache is not null && fileUtc <= _cacheFileUtc)
                return _cache;

            var json = await File.ReadAllTextAsync(source, ct);
            _cache = JsonSerializer.Deserialize<ActionRoutingSpec>(
                         json,
                         new JsonSerializerOptions(JsonSerializerDefaults.Web)
                     ) ?? new ActionRoutingSpec();
            _cacheFileUtc = fileUtc;
            return _cache;
        }
        finally
        {
            _gate.Release();
        }
    }
}
