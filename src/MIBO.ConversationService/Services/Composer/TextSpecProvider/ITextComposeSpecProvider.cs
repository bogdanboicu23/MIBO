using MIBO.ConversationService.DTOs.TextCompose;

namespace MIBO.ConversationService.Services.Composer.TextSpecProvider;

public interface ITextComposeSpecProvider
{
    Task<TextComposeSpec> GetSpecAsync(string conversationId, string userId, CancellationToken ct);
}