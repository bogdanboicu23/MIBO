namespace MIBO.ConversationService.Services.Answer;

public interface IAnswerService
{
    Task<string> AnswerAsync(
        string conversationId,
        string userId,
        string userPrompt,
        object conversationContext,
        CancellationToken ct);
}