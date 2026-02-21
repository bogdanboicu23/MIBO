using System.Text.Json;
using MIBO.ConversationService.DTOs.PlannerContracts;

namespace MIBO.ConversationService.Services.Composer.Ui;

public interface IUiComposer
{
    Task<object> ComposeUiV1Async(
        string conversationId,
        string userId,
        UiIntent uiIntent,
        IReadOnlyDictionary<string, JsonElement> toolResults,
        CancellationToken ct
    );
}