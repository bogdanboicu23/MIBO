using MIBO.ConversationService.DTOs.Actions;
using MIBO.ConversationService.Services.Actions.RoutingSpecProvider;

namespace MIBO.ConversationService.Services.Actions.Router;


public sealed class ActionRouter : IActionRouter
{
    private readonly IActionRoutingSpecProvider _provider;

    public ActionRouter(IActionRoutingSpecProvider provider) => _provider = provider;

    public async Task<ActionRoute> ResolveAsync(string actionType, CancellationToken ct)
    {
        var route = await TryResolveAsync(actionType, ct);
        if (route is null) throw new InvalidOperationException($"No route for action type: {actionType}");
        return route;
    }

    public async Task<ActionRoute?> TryResolveAsync(string actionType, CancellationToken ct)
    {
        var spec = await _provider.GetAsync(ct);

        var exact = spec.Routes.FirstOrDefault(r =>
            string.Equals(r.ActionType, actionType, StringComparison.OrdinalIgnoreCase)
        );
        if (exact is not null) return exact;

        var wildcard = spec.Routes.FirstOrDefault(r =>
            !string.IsNullOrWhiteSpace(r.ActionType) &&
            r.ActionType.EndsWith(".*", StringComparison.Ordinal) &&
            actionType.StartsWith(r.ActionType[..^1], StringComparison.OrdinalIgnoreCase)
        );
        if (wildcard is not null) return wildcard;

        return spec.Routes.FirstOrDefault(r =>
            string.Equals(r.ActionType, "*", StringComparison.OrdinalIgnoreCase)
        );
    }
}
