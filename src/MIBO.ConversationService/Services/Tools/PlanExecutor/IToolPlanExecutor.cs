using System.Text.Json;
using MIBO.ConversationService.DTOs.PlannerContracts;

namespace MIBO.ConversationService.Services.Tools.PlanExecutor;


public interface IToolPlanExecutor
{
    Task<IReadOnlyDictionary<string, JsonElement>> ExecuteAsync(ToolPlanV1 plan, CancellationToken ct);
}