using System.Text.Json;
using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.DTOs.PlannerContracts;
using MIBO.ConversationService.DTOs.Tools;
using MIBO.ConversationService.Services.Tools.BindingResolver;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Tools.PlanExecutor;

public sealed class ToolPlanExecutor : IToolPlanExecutor
{
    private readonly IToolExecutor _tools;
    private readonly IArgBindingResolver _resolver;
    private readonly ChatOrchestratorOptions _opt;

    public ToolPlanExecutor(
        IToolExecutor tools,
        IArgBindingResolver resolver,
        IOptions<ChatOrchestratorOptions> opt
    )
    {
        _tools = tools;
        _resolver = resolver;
        _opt = opt.Value;
    }

    public async Task<IReadOnlyDictionary<string, JsonElement>> ExecuteAsync(ToolPlanV1 plan, CancellationToken ct)
    {
        var results = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        foreach (var step in plan.Steps)
        {
            try
            {
                var resolvedArgs = _resolver.Resolve(step.Tool, step.Args, results);
                var res = await _tools.ExecuteAsync(new ToolCall(step.Tool, resolvedArgs), ct);

                // Keying strategy: by tool name (latest wins). Alternativ: $"{step.Id}:{step.Tool}"
                results[step.Tool] = res.Body;
            }
            catch
            {
                if (!_opt.BestEffortTools) throw;
                // best-effort: ignoră step-ul eșuat și continuă
            }
        }

        return results;
    }
}