using System.Text.Json;
using System.Text.Json.Serialization;

namespace MIBO.ConversationService.DTOs.PlannerContracts;

public sealed class PlannerInputV1
{
    [JsonPropertyName("schema")]
    public string Schema { get; set; } = "planner_input.v1";

    [JsonPropertyName("userPrompt")]
    public string UserPrompt { get; set; } = "";

    [JsonPropertyName("conversationContext")]
    public Dictionary<string, object?> ConversationContext { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("toolCatalog")]
    public PlannerToolCatalog ToolCatalog { get; set; } = new();

    [JsonPropertyName("uiComponentCatalog")]
    public JsonElement UiComponentCatalog { get; set; }

    [JsonPropertyName("constraints")]
    public PlannerInputConstraints Constraints { get; set; } = new();

    [JsonPropertyName("meta")]
    public PlannerInputMeta Meta { get; set; } = new();
}

public sealed class PlannerToolCatalog
{
    [JsonPropertyName("tools")]
    public List<PlannerToolDescriptor> Tools { get; set; } = new();
}

public sealed class PlannerToolDescriptor
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("method")]
    public string Method { get; set; } = "";

    [JsonPropertyName("requiredArgs")]
    public string[] RequiredArgs { get; set; } = Array.Empty<string>();

    [JsonPropertyName("defaultArgs")]
    public Dictionary<string, object?> DefaultArgs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class PlannerInputConstraints
{
    [JsonPropertyName("maxSteps")]
    public int MaxSteps { get; set; } = 8;
}

public sealed class PlannerInputMeta
{
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }
}
