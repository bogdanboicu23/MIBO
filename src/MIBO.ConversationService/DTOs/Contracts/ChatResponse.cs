namespace MIBO.ConversationService.DTOs.Contracts;

public sealed record ChatResponse(
    string Text,
    object? UiV1,
    string CorrelationId,
    string Schema = "chat.response.v2",
    IReadOnlyList<string>? Warnings = null
);
