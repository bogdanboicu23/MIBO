using System.Text.Json;
using MIBO.ConversationService.DTOs.PlannerContracts;
using MIBO.ConversationService.Services.Composer.UiSpecProvider;

namespace MIBO.ConversationService.Services.Composer.Ui;


public sealed class UiComposer : IUiComposer
{
    private readonly IUiComposeSpecProvider _specProvider;

    public UiComposer(IUiComposeSpecProvider specProvider) => _specProvider = specProvider;

    public async Task<object> ComposeUiV1Async(
        string conversationId,
        string userId,
        UiIntent uiIntent,
        IReadOnlyDictionary<string, JsonElement> toolResults,
        CancellationToken ct
    )
    {
        var spec = await _specProvider.GetSpecAsync(conversationId, userId, ct);

        // UI root vine din planner uiIntent.component_tree (nu hardcodăm componente)
        var root = uiIntent.ComponentTree;

        // Optional snapshot data
        Dictionary<string, object?>? data = null;
        if (spec.IncludeToolDataSnapshot)
        {
            data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var (toolRef, body) in toolResults)
            {
                if (spec.AllowedToolRefs.Count > 0 && !spec.AllowedToolRefs.Contains(toolRef, StringComparer.OrdinalIgnoreCase))
                    continue;

                data[toolRef] = JsonSerializer.Deserialize<object>(
                    body.GetRawText(),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)
                );
            }
        }

        var ui = new Dictionary<string, object?>
        {
            ["schema"] = spec.Schema,
            ["root"] = root
        };

        if (data is not null) ui["data"] = data;

        if (spec.IncludeBindings) ui["bindings"] = uiIntent.Bindings;
        if (spec.IncludeSubscriptions) ui["subscriptions"] = uiIntent.Subscriptions;

        return ui;
    }
}