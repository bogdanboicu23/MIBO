using MIBO.ConversationService.DTOs.Actions;

namespace MIBO.ConversationService.Services.Actions.Patch;

public interface IActionPatchComposer
{
    object ComposeDataPatch(
        ActionRoute route,
        string selectedTool,
        object? toolBody,
        ActionEnvelopeV1 action
    );
}
