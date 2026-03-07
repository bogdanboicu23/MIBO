using MIBO.ConversationService.DTOs.Actions;

namespace MIBO.ConversationService.Services.Actions.Handler;

public sealed record ActionResultV1(string Schema, object? UiPatch, string? Text, object? UiV1 = null, string? CorrelationId = null);

public interface IActionHandler
{
    Task<ActionResultV1> HandleAsync(ActionEnvelopeV1 action, CancellationToken ct);
}
