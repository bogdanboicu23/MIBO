using System.Text.Json;

namespace MIBO.ConversationService.Services.Composer.Text;

public interface ITextComposer
{
    Task<string> ComposeTextAsync(
        string conversationId,
        string userId,
        string userPrompt,
        IReadOnlyDictionary<string, JsonElement> toolResults,
        CancellationToken ct
    );
}