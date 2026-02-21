using System.Text.Json;

namespace MIBO.ConversationService.DTOs.Tools;

public sealed record ToolExecutionContext(
    string CorrelationId,
    string UserId,
    string ConversationId
);

public sealed record ToolCall(
    string Tool,
    Dictionary<string, object?> Args
);

public sealed record ToolResult(
    string Tool,
    int StatusCode,
    JsonElement Body,
    IReadOnlyDictionary<string, string>? Headers = null
);