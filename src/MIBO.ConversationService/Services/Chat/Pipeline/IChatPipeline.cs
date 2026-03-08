namespace MIBO.ConversationService.Services.Chat.Pipeline;

public interface IChatPipeline
{
    Task ExecuteAsync(ChatPipelineContext context, CancellationToken ct);
}
