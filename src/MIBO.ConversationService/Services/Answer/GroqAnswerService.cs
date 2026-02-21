using MIBO.ConversationService.Services.GroqChat;

namespace MIBO.ConversationService.Services.Answer;

public sealed class GroqAnswerService : IAnswerService
{
    private readonly IGroqChatService _groq;

    public GroqAnswerService(IGroqChatService groq) => _groq = groq;

    public async Task<string> AnswerAsync(
        string conversationId,
        string userId,
        string userPrompt,
        object conversationContext,
        CancellationToken ct)
    {
        return await _groq.SendMessageAsync(userPrompt, ct);
    }
}