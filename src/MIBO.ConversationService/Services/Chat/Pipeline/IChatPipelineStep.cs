namespace MIBO.ConversationService.Services.Chat.Pipeline;

public interface IChatPipelineStep
{
    string Name { get; }

    Task ExecuteAsync(ChatPipelineContext context, CancellationToken ct);
}
