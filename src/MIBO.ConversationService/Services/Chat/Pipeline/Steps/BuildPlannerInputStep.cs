using MIBO.ConversationService.Services.Planner.Factory;

namespace MIBO.ConversationService.Services.Chat.Pipeline.Steps;

public sealed class BuildPlannerInputStep : IChatPipelineStep
{
    private readonly IPlannerInputFactory _plannerInputFactory;

    public BuildPlannerInputStep(IPlannerInputFactory plannerInputFactory)
        => _plannerInputFactory = plannerInputFactory;

    public string Name => "build_planner_input";

    public async Task ExecuteAsync(ChatPipelineContext context, CancellationToken ct)
    {
        if (context.SkipPlanning) return;

        context.PlannerInput = await _plannerInputFactory.BuildAsync(
            context.Request.ConversationId,
            context.Request.UserId,
            context.Request.Prompt,
            context.ConversationContext,
            ct
        );
    }
}
