using System.Diagnostics;

namespace MIBO.ConversationService.Services.Chat.Pipeline;

public sealed class ChatPipeline : IChatPipeline
{
    private readonly IReadOnlyList<IChatPipelineStep> _steps;
    private readonly ILogger<ChatPipeline> _logger;

    public ChatPipeline(IEnumerable<IChatPipelineStep> steps, ILogger<ChatPipeline> logger)
    {
        _steps = steps.ToArray();
        _logger = logger;
    }

    public async Task ExecuteAsync(ChatPipelineContext context, CancellationToken ct)
    {
        foreach (var step in _steps)
        {
            var sw = Stopwatch.StartNew();
            await step.ExecuteAsync(context, ct);
            sw.Stop();

            _logger.LogInformation(
                "Chat pipeline step completed: {Step} in {ElapsedMs}ms (correlationId={CorrelationId})",
                step.Name,
                sw.ElapsedMilliseconds,
                context.CorrelationId
            );
        }
    }
}
