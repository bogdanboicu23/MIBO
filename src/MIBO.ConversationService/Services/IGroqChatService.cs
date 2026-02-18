namespace MIBO.ConversationService.Services;

public interface IGroqChatService
{
    IAsyncEnumerable<string> StreamMessageAsync(string message, CancellationToken ct = default);
}