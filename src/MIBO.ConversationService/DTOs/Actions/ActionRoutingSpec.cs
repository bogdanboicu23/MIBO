namespace MIBO.ConversationService.DTOs.Actions;

public sealed class ActionRoutingSpec
{
    public List<ActionRoute> Routes { get; set; } = new();
}

public sealed class ActionRoute
{
    // action type (ex: "shop.buy")
    public string ActionType { get; set; } = default!;

    // tool call (ex: "shop.buyProduct") — vine din tools.json (registry)
    public string Tool { get; set; } = default!;

    // map payload keys -> tool args keys (decuplat)
    public Dictionary<string, string> ArgMap { get; set; } = new();

    // Optional: publish event after tool success
    public PublishSpec? Publish { get; set; }
}


public sealed class PublishSpec
{
    public string Subject { get; set; } = default!;

    public Dictionary<string, string> FromActionPayload { get; set; } = new();

    // NEW: extract from tool result JSON (by jsonPathLite or jsonPath)
    // ex: { "amount": "$.total", "currency": "$.currency" }
    public Dictionary<string, string> FromToolResultJsonPath { get; set; } = new();

    public Dictionary<string, object?> Fixed { get; set; } = new();
}