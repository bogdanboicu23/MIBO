using MIBO.ConversationService.DTOs.UiCompose;

namespace MIBO.ConversationService.Services.Composer.UiSpecProvider;

public interface IUiComposeSpecProvider
{
    Task<UiComposeSpec> GetSpecAsync(string conversationId, string userId, CancellationToken ct);
}