// /backend/src/Application/Planner/PlannerInputFactory.cs

using MIBO.ConversationService.Services.Tools;
using MIBO.ConversationService.Services.UI;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Planner.Factory;

public sealed class PlannerOptions
{
    public int MaxToolSteps { get; set; } = 8;
}

public sealed class PlannerInputFactory : IPlannerInputFactory
{
    private readonly IToolRegistry _tools;
    private readonly IUiCatalogProvider _uiCatalog;
    private readonly PlannerOptions _opt;

    public PlannerInputFactory(
        IToolRegistry tools,
        IUiCatalogProvider uiCatalog,
        IOptions<PlannerOptions> opt
    )
    {
        _tools = tools;
        _uiCatalog = uiCatalog;
        _opt = opt.Value;
    }

    public async Task<object> BuildAsync(
        string conversationId,
        string userId,
        string userPrompt,
        object conversationContext,
        CancellationToken ct
    )
    {
        // toolCatalog (authoritative): derivat din ToolRegistry (care e alimentat de provider)
        var toolCatalog = new
        {
            tools = _tools.All().Select(t => new
            {
                name = t.Name,
                method = t.Method,
                requiredArgs = t.RequiredArgs
            }).ToArray()
        };

        // uiComponentCatalog (authoritative): provider decuplat (json/db/config service)
        var uiComponentCatalog = await _uiCatalog.GetCatalogAsync(ct);

        // planner_input.v1: include userId doar ca metadată (nu e musai, dar util)
        return new
        {
            schema = "planner_input.v1",
            userPrompt,
            conversationContext,
            toolCatalog,
            uiComponentCatalog,
            constraints = new
            {
                maxSteps = _opt.MaxToolSteps
            },
            meta = new
            {
                conversationId,
                userId
            }
        };
    }
}