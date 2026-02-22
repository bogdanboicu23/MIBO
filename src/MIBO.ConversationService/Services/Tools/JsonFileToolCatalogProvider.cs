// /backend/src/Infrastructure/Tools/JsonFileToolCatalogProvider.cs

using System.Text.Json;
using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.DTOs.Tools;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Tools;

public sealed class JsonFileToolCatalogProvider : IToolCatalogProvider
{
    private readonly ToolCatalogOptions _opt;

    public JsonFileToolCatalogProvider(IOptions<ToolCatalogOptions> opt)
        => _opt = opt.Value;

    public async Task<IReadOnlyCollection<ToolDefinition>> GetToolsAsync(CancellationToken ct)
    {
        // Source = path local (volume mounted / configmap)
        var json = await File.ReadAllTextAsync(_opt.Source, ct);
        using var doc = JsonDocument.Parse(json);

        var toolsEl = doc.RootElement.GetProperty("tools");
        var defs = new List<ToolDefinition>();

        foreach (var t in toolsEl.EnumerateArray())
        {
            defs.Add(new ToolDefinition(
                Name: t.GetProperty("name").GetString()!,
                Method: t.GetProperty("method").GetString()!,
                UrlTemplate: t.GetProperty("urlTemplate").GetString()!,
                TimeoutMs: t.TryGetProperty("timeoutMs", out var to) ? to.GetInt32() : 0,
                RetryCount: t.TryGetProperty("retryCount", out var rc) ? rc.GetInt32() : 0,
                CacheTtlSeconds: t.TryGetProperty("cacheTtlSeconds", out var ttl) ? ttl.GetInt32() : 0,
                RequiredArgs: t.TryGetProperty("requiredArgs", out var ra)
                    ? ra.EnumerateArray().Select(x => x.GetString()!).ToArray()
                    : Array.Empty<string>()
            ));
        }

        return defs;
    }
}