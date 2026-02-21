using MIBO.ConversationService.DTOs.Actions;
using MIBO.ConversationService.Services.Actions.RoutingSpecProvider;

namespace MIBO.ConversationService.Services.Actions.Router;


public sealed class ActionRouter : IActionRouter
{
    private readonly IActionRoutingSpecProvider _provider;

    public ActionRouter(IActionRoutingSpecProvider provider) => _provider = provider;

    public async Task<ActionRoute> ResolveAsync(string actionType, CancellationToken ct)
    {
        var spec = await _provider.GetAsync(ct);
        var route = spec.Routes.FirstOrDefault(r => string.Equals(r.ActionType, actionType, StringComparison.OrdinalIgnoreCase));
        if (route is null) throw new InvalidOperationException($"No route for action type: {actionType}");
        return route;
    }
}