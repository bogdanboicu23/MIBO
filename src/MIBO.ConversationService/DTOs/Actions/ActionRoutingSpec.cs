namespace MIBO.ConversationService.DTOs.Actions;

public sealed class ActionRoutingSpec
{
    public List<ActionRoute> Routes { get; set; } = new();
}

public sealed class ActionRoute
{
    // action type (ex: "shop.buy")
    public string ActionType { get; set; } = default!;

    // "tool" | "prompt"
    public string Mode { get; set; } = "tool";

    // tool call (ex: "shop.buyProduct") — vine din tools.json (registry)
    public string Tool { get; set; } = "";

    // prompt route support (when Mode="prompt")
    public string PromptTemplate { get; set; } = "";


    // map payload keys -> tool args keys (decuplat)
    public Dictionary<string, string> ArgMap { get; set; } = new();

    // arg templates resolved against payload/ui/context (ex: "{{payload.query}}")
    public Dictionary<string, object?> ArgTemplate { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // Optional multi-tool selection by payload conditions.
    public List<ActionToolCandidate> ToolCandidates { get; set; } = new();

    // UI patch destination. Default: /data/{tool}
    public string PatchPath { get; set; } = "/data/{tool}";

    // Additional paths to keep aliases synced.
    public List<string> PatchAliases { get; set; } = new();

    // Optional lightweight text returned together with patch.
    public string? SuccessText { get; set; }

    // Optional: publish event after tool success
    public PublishSpec? Publish { get; set; }
}

public sealed class ActionToolCandidate
{
    public string Tool { get; set; } = "";

    public List<string> WhenPayloadHasAny { get; set; } = new();

    public List<string> WhenPayloadHasAll { get; set; } = new();

    public Dictionary<string, string> WhenPayloadEquals { get; set; } = new(StringComparer.OrdinalIgnoreCase);
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
