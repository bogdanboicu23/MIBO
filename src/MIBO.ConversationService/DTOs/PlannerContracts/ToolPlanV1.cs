using System.Text.Json.Serialization;

namespace MIBO.ConversationService.DTOs.PlannerContracts;

public sealed class ToolPlanV1
{
    [JsonPropertyName("schema")]
    public string Schema { get; set; } = "tool_plan.v1";

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = "";

    [JsonPropertyName("steps")]
    public List<ToolStep> Steps { get; set; } = new();

    [JsonPropertyName("uiIntent")]
    public UiIntent? UiIntent { get; set; }
}

public sealed class ToolStep
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("tool")]
    public string Tool { get; set; } = "";

    [JsonPropertyName("args")]
    public Dictionary<string, object?> Args { get; set; } = new();

    [JsonPropertyName("cache_ttl_seconds")]
    public int? CacheTtlSeconds { get; set; }
}

public sealed class UiIntent
{
    [JsonPropertyName("component_tree")]
    public Dictionary<string, object?> ComponentTree { get; set; } = new();

    [JsonPropertyName("bindings")]
    public List<Dictionary<string, object?>> Bindings { get; set; } = new();

    [JsonPropertyName("subscriptions")]
    public List<Dictionary<string, object?>> Subscriptions { get; set; } = new();
}