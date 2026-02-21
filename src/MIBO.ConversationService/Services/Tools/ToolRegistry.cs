using System.Collections.Concurrent;
using System.Text.Json;
using MIBO.ConversationService.DTOs.Tools;

namespace MIBO.ConversationService.Services.Tools;

public sealed class ToolRegistry : IToolRegistry
{
    private readonly IToolCatalogProvider _provider;
    private readonly ConcurrentDictionary<string, ToolDefinition> _tools = new(StringComparer.OrdinalIgnoreCase);

    public ToolRegistry(IToolCatalogProvider provider) => _provider = provider;

    public async Task RefreshAsync(CancellationToken ct)
    {
        var tools = await _provider.GetToolsAsync(ct);
        _tools.Clear();
        foreach (var t in tools) _tools[t.Name] = t;
    }

    public ToolDefinition Get(string name)
        => _tools.TryGetValue(name, out var def) ? def : throw new KeyNotFoundException($"Tool not found: {name}");

    public bool TryGet(string name, out ToolDefinition? def)
        => _tools.TryGetValue(name, out def);

    public IReadOnlyCollection<ToolDefinition> All()
        => _tools.Values.ToArray();
}