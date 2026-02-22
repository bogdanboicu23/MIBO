namespace MIBO.ConversationService.DTOs.Actions;

public sealed class ActionEnvelopeV1
{
    public string Schema { get; set; } = "action.v1";
    public string ConversationId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public ActionPayload Action { get; set; } = new();
    public UiContext? UiContext { get; set; }
}

public sealed class ActionPayload
{
    public string Type { get; set; } = default!; // ex: "shop.buy" | "ui.setFilter" etc.
    public Dictionary<string, object?> Payload { get; set; } = new();
}

public sealed class UiContext
{
    public string? UiInstanceId { get; set; }
    public string? FocusedComponentId { get; set; }
}