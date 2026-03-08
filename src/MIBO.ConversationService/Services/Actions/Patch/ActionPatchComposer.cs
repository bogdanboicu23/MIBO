using MIBO.ConversationService.DTOs.Actions;
using MIBO.ConversationService.Services.UI.PatchBuilder;

namespace MIBO.ConversationService.Services.Actions.Patch;

public sealed class ActionPatchComposer : IActionPatchComposer
{
    public object ComposeDataPatch(
        ActionRoute route,
        string selectedTool,
        object? toolBody,
        ActionEnvelopeV1 action
    )
    {
        var paths = new List<string>
        {
            ResolvePath(route.PatchPath, selectedTool, action)
        };

        foreach (var alias in route.PatchAliases)
        {
            var p = ResolvePath(alias, selectedTool, action);
            if (string.IsNullOrWhiteSpace(p)) continue;
            if (paths.Contains(p, StringComparer.OrdinalIgnoreCase)) continue;
            paths.Add(p);
        }

        var ops = paths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => UiPatchBuilder.Replace(p, toolBody))
            .ToArray();

        return UiPatchBuilder.Patch(
            uiInstanceId: action.UiContext?.UiInstanceId,
            ops
        );
    }

    private static string ResolvePath(string template, string selectedTool, ActionEnvelopeV1 action)
    {
        var path = string.IsNullOrWhiteSpace(template) ? "/data/{tool}" : template;
        path = path.Replace("{tool}", selectedTool, StringComparison.OrdinalIgnoreCase);
        path = path.Replace("{{context.conversationId}}", action.ConversationId, StringComparison.OrdinalIgnoreCase);
        path = path.Replace("{{context.userId}}", action.UserId, StringComparison.OrdinalIgnoreCase);
        return path.StartsWith('/') ? path : "/" + path;
    }
}
