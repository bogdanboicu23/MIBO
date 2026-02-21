using MIBO.ConversationService.DTOs.Contracts;

namespace MIBO.ConversationService.Services.Chat;

public interface IChatOrchestrator
{
    Task<ChatResponse> HandleAsync(ChatRequest req, CancellationToken ct);
}