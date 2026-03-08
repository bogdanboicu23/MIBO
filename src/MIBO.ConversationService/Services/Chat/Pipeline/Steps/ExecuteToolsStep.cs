using System.Text.Json;
using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.Services.Planner.Fallback;
using MIBO.ConversationService.Services.Tools.PlanExecutor;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Chat.Pipeline.Steps;

public sealed class ExecuteToolsStep : IChatPipelineStep
{
    private readonly IToolPlanExecutor _planExecutor;
    private readonly IFallbackPlanner _fallbackPlanner;
    private readonly ChatOrchestratorOptions _opt;
    private readonly ILogger<ExecuteToolsStep> _logger;

    public ExecuteToolsStep(
        IToolPlanExecutor planExecutor,
        IFallbackPlanner fallbackPlanner,
        IOptions<ChatOrchestratorOptions> opt,
        ILogger<ExecuteToolsStep> logger
    )
    {
        _planExecutor = planExecutor;
        _fallbackPlanner = fallbackPlanner;
        _opt = opt.Value;
        _logger = logger;
    }

    public string Name => "execute_tools";

    public async Task ExecuteAsync(ChatPipelineContext context, CancellationToken ct)
    {
        if (context.SkipPlanning)
        {
            context.ToolResults = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        try
        {
            context.ToolResults = await _planExecutor.ExecuteAsync(context.Plan, ct);
        }
        catch (Exception ex) when (_opt.EnableFallback)
        {
            _logger.LogWarning(
                ex,
                "Tool plan execution failed, switching to empty fallback execution (correlationId={CorrelationId})",
                context.CorrelationId
            );

            context.Warnings.Add("tool_execution_fallback");
            context.Plan = await _fallbackPlanner.CreateFallbackPlanAsync(context.Request.Prompt, ct);
            try
            {
                context.ToolResults = await _planExecutor.ExecuteAsync(context.Plan, ct);
            }
            catch (Exception fallbackEx) when (_opt.BestEffortTools || _opt.EnableFallback)
            {
                _logger.LogWarning(
                    fallbackEx,
                    "Fallback tool execution failed (correlationId={CorrelationId})",
                    context.CorrelationId
                );
                context.Warnings.Add("tool_execution_fallback_failed");
                context.ToolResults = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
