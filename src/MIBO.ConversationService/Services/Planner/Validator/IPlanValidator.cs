using MIBO.ConversationService.DTOs.PlannerContracts;

namespace MIBO.ConversationService.Services.Planner.Validator;

public interface IPlanValidator
{
    void ValidateOrThrow(ToolPlanV1 plan);
}