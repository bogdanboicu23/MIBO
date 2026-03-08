using MIBO.ConversationService.DTOs.Contracts;
using MIBO.ConversationService.Services.Chat.Pipeline;

namespace MIBO.ConversationService.Services.Chat;

public sealed class ChatOrchestrator : IChatOrchestrator
{
    private readonly IChatPipeline _pipeline;
    private readonly ILogger<ChatOrchestrator> _logger;

    public ChatOrchestrator(
        IChatPipeline pipeline,
        ILogger<ChatOrchestrator> logger
    )
    {
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task<ChatResponse> HandleAsync(ChatRequest req, CancellationToken ct)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["ConversationId"] = req.ConversationId,
            ["UserId"] = req.UserId
        });

        var context = new ChatPipelineContext(req, correlationId);
        await _pipeline.ExecuteAsync(context, ct);

        return new ChatResponse(
            Text: context.Text,
            UiV1: context.UiV1,
            CorrelationId: correlationId,
            Schema: "chat.response.v2",
            Warnings: context.Warnings
        );
    }
}
