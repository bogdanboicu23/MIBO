using MIBO.ConversationService.DTOs.Actions;

namespace MIBO.ConversationService.Services.Actions.Router;

public interface IActionRouter
{
    Task<ActionRoute> ResolveAsync(string actionType, CancellationToken ct);
}