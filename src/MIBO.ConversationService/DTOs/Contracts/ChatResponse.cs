namespace MIBO.ConversationService.DTOs.Contracts;

public sealed record ChatResponse(
    string Text,
    object? UiV1,
    string CorrelationId
);