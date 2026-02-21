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

    public JsonFileActionRoutingSpecProvider(IOptions<ActionRoutingOptions> opt) => _opt = opt.Value;

    public async Task<ActionRoutingSpec> GetAsync(CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(_opt.Source, ct);
        return JsonSerializer.Deserialize<ActionRoutingSpec>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
               ?? new ActionRoutingSpec();
    }
}