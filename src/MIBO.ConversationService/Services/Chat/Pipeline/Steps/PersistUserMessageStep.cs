using MIBO.Storage.Mongo.Store.Conversation;

namespace MIBO.ConversationService.Services.Chat.Pipeline.Steps;

public sealed class PersistUserMessageStep : IChatPipelineStep
{
    private readonly IConversationStore _store;
    private readonly ILogger<PersistUserMessageStep> _logger;

    public PersistUserMessageStep(IConversationStore store, ILogger<PersistUserMessageStep> logger)
    {
        _store = store;
        _logger = logger;
    }

    public string Name => "persist_user_message";

    public Task ExecuteAsync(ChatPipelineContext context, CancellationToken ct)
    {
        // Fire-and-forget: user message persistence should not block the pipeline.
        // The rest of the pipeline (planner, tools, compose) doesn't depend on this write.
        _ = PersistInBackgroundAsync(
            context.Request.ConversationId,
            context.Request.UserId,
            context.Request.Prompt,
            context.CorrelationId
        );

        return Task.CompletedTask;
    }

    private async Task PersistInBackgroundAsync(
        string conversationId, string userId, string prompt, string correlationId)
    {
        try
        {
            await _store.AppendUserMessageAsync(conversationId, userId, prompt, correlationId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Background persist of user message failed for conversationId={ConversationId}",
                conversationId);
        }
    }
}