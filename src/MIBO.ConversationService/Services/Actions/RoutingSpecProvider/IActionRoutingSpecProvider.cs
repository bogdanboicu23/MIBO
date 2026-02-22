using MIBO.ConversationService.DTOs.Actions;

namespace MIBO.ConversationService.Services.Actions.RoutingSpecProvider;

public interface IActionRoutingSpecProvider
{
    Task<ActionRoutingSpec> GetAsync(CancellationToken ct);
}