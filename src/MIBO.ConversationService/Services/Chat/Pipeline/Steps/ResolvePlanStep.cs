using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.Services.Planner.Client;
using MIBO.ConversationService.Services.Planner.Fallback;
using MIBO.ConversationService.Services.Planner.Validator;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Chat.Pipeline.Steps;

public sealed class ResolvePlanStep : IChatPipelineStep
{
    private readonly IPlannerClient _plannerClient;
    private readonly IPlanValidator _planValidator;
    private readonly IFallbackPlanner _fallbackPlanner;
    private readonly ChatOrchestratorOptions _opt;
    private readonly ILogger<ResolvePlanStep> _logger;

    public ResolvePlanStep(
        IPlannerClient plannerClient,
        IPlanValidator planValidator,
        IFallbackPlanner fallbackPlanner,
        IOptions<ChatOrchestratorOptions> opt,
        ILogger<ResolvePlanStep> logger
    )
    {
        _plannerClient = plannerClient;
        _planValidator = planValidator;
        _fallbackPlanner = fallbackPlanner;
        _opt = opt.Value;
        _logger = logger;
    }

    public string Name => "resolve_plan";

    public async Task ExecuteAsync(ChatPipelineContext context, CancellationToken ct)
    {
        if (context.SkipPlanning) return;

        try
        {
            if (context.PlannerInput is null)
                throw new InvalidOperationException("Planner input missing before resolve_plan step");

            context.Plan = await _plannerClient.PlanAsync(context.PlannerInput, ct);
            _planValidator.ValidateOrThrow(context.Plan);
        }
        catch (Exception ex) when (_opt.EnableFallback)
        {
            _logger.LogWarning(
                ex,
                "Planner failed, switching to fallback plan (correlationId={CorrelationId})",
                context.CorrelationId
            );

            context.Warnings.Add("planner_fallback");
            context.Plan = await _fallbackPlanner.CreateFallbackPlanAsync(context.Request.Prompt, ct);
            _planValidator.ValidateOrThrow(context.Plan);
        }
    }
}
