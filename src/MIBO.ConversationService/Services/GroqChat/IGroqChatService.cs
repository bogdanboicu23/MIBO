namespace MIBO.ConversationService.Services.GroqChat;

public interface IGroqChatService
{
    IAsyncEnumerable<string> StreamMessageAsync(string message, CancellationToken ct = default);
    Task<string> SendMessageAsync(string message, CancellationToken ct = default);
}