using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.Services.Tools;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Background;

public sealed class ToolRegistryRefresher : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IToolRegistry _registry;
    private readonly ToolCatalogOptions _opt;

    public ToolRegistryRefresher(IToolRegistry registry, IOptions<ToolCatalogOptions> opt)
    {
        _registry = registry;
        _opt = opt.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // initial load
        await _registry.RefreshAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(_opt.ReloadSeconds), stoppingToken);
            await _registry.RefreshAsync(stoppingToken);
        }
    }
}