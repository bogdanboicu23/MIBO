namespace MIBO.ConversationService.DTOs.Contracts;

public sealed record ChatRequest(
    string ConversationId,
    string UserId,
    string Prompt
);