using Microsoft.AspNetCore.SignalR;

namespace MIBO.ConversationService.Hubs;

public interface IUiClient
{
    Task UiPatch(object patch);
}

public sealed class UiHub : Hub<IUiClient>
{
    public Task JoinConversation(string conversationId)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");

    public Task LeaveConversation(string conversationId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");
}