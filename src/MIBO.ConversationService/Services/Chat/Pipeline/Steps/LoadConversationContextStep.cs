using MIBO.Storage.Mongo.Store.Conversation;

namespace MIBO.ConversationService.Services.Chat.Pipeline.Steps;

public sealed class LoadConversationContextStep : IChatPipelineStep
{
    private readonly IConversationStore _store;

    public LoadConversationContextStep(IConversationStore store) => _store = store;

    public string Name => "load_conversation_context";

    public async Task ExecuteAsync(ChatPipelineContext context, CancellationToken ct)
    {
        var persisted = await _store.GetPlannerConversationContextAsync(
            context.Request.ConversationId,
            context.Request.UserId,
            ct
        );

        context.ConversationContext = MergeConversationContext(persisted, context.Request);
    }

    private static Dictionary<string, object?> MergeConversationContext(
        Dictionary<string, object?> persistedContext,
        DTOs.Contracts.ChatRequest req)
    {
        var merged = new Dictionary<string, object?>(persistedContext, StringComparer.OrdinalIgnoreCase);

        if (req.ClientContext is { Count: > 0 })
            merged["clientContext"] = req.ClientContext;

        if (req.ExternalContextUrls is { Count: > 0 })
            merged["externalContextUrls"] = req.ExternalContextUrls;

        return merged;
    }
}
