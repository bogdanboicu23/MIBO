using MIBO.Storage.Mongo.Store.Conversation;

namespace MIBO.ConversationService.Services.Chat.Pipeline.Steps;

public sealed class PersistUserMessageStep : IChatPipelineStep
{
    private readonly IConversationStore _store;

    public PersistUserMessageStep(IConversationStore store) => _store = store;

    public string Name => "persist_user_message";

    public async Task ExecuteAsync(ChatPipelineContext context, CancellationToken ct)
    {
        await _store.AppendUserMessageAsync(
            context.Request.ConversationId,
            context.Request.UserId,
            context.Request.Prompt,
            context.CorrelationId,
            ct
        );
    }
}
