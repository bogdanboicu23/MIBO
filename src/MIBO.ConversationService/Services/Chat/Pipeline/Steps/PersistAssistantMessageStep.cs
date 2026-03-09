using MIBO.Storage.Mongo.Store.Conversation;
using MIBO.Storage.Mongo.Store.Ui;

namespace MIBO.ConversationService.Services.Chat.Pipeline.Steps;

public sealed class PersistAssistantMessageStep : IChatPipelineStep
{
    private readonly IConversationStore _store;
    private readonly IUiInstanceStore _uiInstanceStore;

    public PersistAssistantMessageStep(
        IConversationStore store,
        IUiInstanceStore uiInstanceStore
    )
    {
        _store = store;
        _uiInstanceStore = uiInstanceStore;
    }

    public string Name => "persist_assistant_message";

    public async Task ExecuteAsync(ChatPipelineContext context, CancellationToken ct)
    {
        if (context.StreamMode) return;

        var messageTask = _store.AppendAssistantMessageAsync(
            context.Request.ConversationId,
            context.Request.UserId,
            context.Text,
            context.UiV1,
            context.CorrelationId,
            ct
        );

        if (context.UiV1 is null)
        {
            await messageTask;
            return;
        }

        // Run both MongoDB writes concurrently
        var uiTask = _uiInstanceStore.UpsertFromAssistantMessageAsync(
            context.Request.ConversationId,
            context.Request.UserId,
            context.UiV1,
            ct
        );

        await Task.WhenAll(messageTask, uiTask);
    }
}