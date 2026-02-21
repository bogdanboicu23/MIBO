using System.Text.Json;
using MIBO.ConversationService.DTOs.PlannerContracts;

namespace MIBO.ConversationService.Services.Planner.Client;


public sealed class LangChainPlannerClient : IPlannerClient
{
    private readonly HttpClient _http;
    private readonly ILogger<LangChainPlannerClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public LangChainPlannerClient(HttpClient http, ILogger<LangChainPlannerClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ToolPlanV1> PlanAsync(object plannerInput, CancellationToken ct)
    {
        using var resp = await _http.PostAsJsonAsync("/v1/plan", plannerInput, JsonOpts, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("Planner error {Status}: {Body}", (int)resp.StatusCode, err);
        }
        resp.EnsureSuccessStatusCode();

        var plan = await resp.Content.ReadFromJsonAsync<ToolPlanV1>(JsonOpts, ct);
        if (plan is null) throw new InvalidOperationException("Planner returned empty response");
        if (!string.Equals(plan.Schema, "tool_plan.v1", StringComparison.Ordinal))
            throw new InvalidOperationException($"Invalid schema: {plan.Schema}");
        return plan;
    }
}