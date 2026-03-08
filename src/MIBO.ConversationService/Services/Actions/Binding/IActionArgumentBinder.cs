using MIBO.ConversationService.DTOs.Actions;
using MIBO.ConversationService.DTOs.Tools;

namespace MIBO.ConversationService.Services.Actions.Binding;

public interface IActionArgumentBinder
{
    Dictionary<string, object?> Bind(ActionRoute route, ActionEnvelopeV1 action, ToolDefinition toolDefinition);
}
