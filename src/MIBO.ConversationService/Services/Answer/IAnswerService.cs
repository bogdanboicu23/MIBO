namespace MIBO.ConversationService.Services.Answer;

public interface IAnswerService
{
    Task<string> AnswerAsync(
        string conversationId,
        string userId,
        string userPrompt,
        object conversationContext,
        CancellationToken ct);

    IAsyncEnumerable<string> StreamAnswerAsync(
        string conversationId,
        string userId,
        string userPrompt,
        object conversationContext,
        CancellationToken ct);
}