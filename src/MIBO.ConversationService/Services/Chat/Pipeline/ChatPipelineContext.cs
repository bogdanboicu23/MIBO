using System.Text.Json;
using MIBO.ConversationService.DTOs.Contracts;
using MIBO.ConversationService.DTOs.PlannerContracts;

namespace MIBO.ConversationService.Services.Chat.Pipeline;

public sealed class ChatPipelineContext
{
    public ChatPipelineContext(ChatRequest request, string correlationId)
    {
        Request = request;
        CorrelationId = correlationId;
    }

    public ChatRequest Request { get; }

    public string CorrelationId { get; }

    public Dictionary<string, object?> ConversationContext { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public PlannerInputV1? PlannerInput { get; set; }

    public ToolPlanV1 Plan { get; set; } = new();

    public IReadOnlyDictionary<string, JsonElement> ToolResults { get; set; }
        = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

    public object? UiV1 { get; set; }

    public string Text { get; set; } = "";

    public List<string> Warnings { get; } = new();

    public bool SkipPlanning { get; set; }

    /// <summary>
    /// When true, ComposeTextStep and PersistAssistantMessageStep are skipped.
    /// The orchestrator handles text streaming and persistence manually.
    /// </summary>
    public bool StreamMode { get; set; }
}
