namespace MIBO.Storage.Mongo.Store.Ui;

public sealed record UiInstanceRef(string UiInstanceId, string ConversationId, string UserId);

public interface IUiInstanceStore
{
    Task UpsertFromAssistantMessageAsync(string conversationId, string userId, object uiV1, CancellationToken ct);
    Task<IReadOnlyList<UiInstanceRef>> FindAffectedAsync(string conversationId, string userId, string @event, CancellationToken ct);

    Task SavePatchAsync(string uiInstanceId, object patch, CancellationToken ct);
}