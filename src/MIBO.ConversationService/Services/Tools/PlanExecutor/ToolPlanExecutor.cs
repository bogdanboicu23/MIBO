using System.Text.Json;
using System.Text.RegularExpressions;
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

    private static readonly Regex BindingRefRegex = new(
        @"\$\{([^}]+)\}|\{\{([^}]+)\}\}",
        RegexOptions.Compiled);

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

        if (plan.Steps is not { Count: > 0 })
            return results;

        // Build execution waves: group steps that can run in parallel
        var waves = BuildExecutionWaves(plan.Steps);

        foreach (var wave in waves)
        {
            if (wave.Count == 1)
            {
                // Single step — no overhead of Task.WhenAll
                await ExecuteStep(wave[0], results, ct);
            }
            else
            {
                // Multiple independent steps — run in parallel
                var tasks = wave.Select(step => ExecuteStep(step, results, ct)).ToArray();
                await Task.WhenAll(tasks);
            }
        }

        return results;
    }

    private async Task ExecuteStep(ToolStep step, Dictionary<string, JsonElement> results, CancellationToken ct)
    {
        try
        {
            var resolvedArgs = _resolver.Resolve(step.Tool, step.Args, results);
            var res = await _tools.ExecuteAsync(new ToolCall(step.Tool, resolvedArgs), ct);

            lock (results)
            {
                results[step.Tool] = res.Body;
                if (!string.IsNullOrWhiteSpace(step.Id))
                    results[step.Id] = res.Body;
            }
        }
        catch (KeyNotFoundException)
        {
            // Unknown tools (planner hallucinations) — skip silently
        }
        catch
        {
            if (!_opt.BestEffortTools) throw;
            // best-effort: skip failed step
        }
    }

    /// <summary>
    /// Analyzes step dependencies and groups independent steps into waves.
    /// Steps within a wave can run concurrently. Waves execute sequentially.
    /// </summary>
    private static List<List<ToolStep>> BuildExecutionWaves(IReadOnlyList<ToolStep> steps)
    {
        var waves = new List<List<ToolStep>>();
        // Track which keys (tool names and step IDs) are produced by which wave
        var producedByWave = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var step in steps)
        {
            var dependencies = ExtractDependencies(step);
            var earliestWave = 0;

            // This step must go after the latest wave that produces any of its dependencies
            foreach (var dep in dependencies)
            {
                if (producedByWave.TryGetValue(dep, out var waveIndex))
                    earliestWave = Math.Max(earliestWave, waveIndex + 1);
            }

            // Ensure the wave list is large enough
            while (waves.Count <= earliestWave)
                waves.Add(new List<ToolStep>());

            waves[earliestWave].Add(step);

            // Record that this step's outputs are produced by this wave
            var currentWave = earliestWave;
            producedByWave[step.Tool] = currentWave;
            if (!string.IsNullOrWhiteSpace(step.Id))
                producedByWave[step.Id] = currentWave;
        }

        return waves;
    }

    /// <summary>
    /// Extracts tool/step references from a step's args by scanning for
    /// ${...} and {{...}} binding tokens.
    /// </summary>
    private static HashSet<string> ExtractDependencies(ToolStep step)
    {
        var deps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (step.Args is null || step.Args.Count == 0) return deps;

        var argsJson = JsonSerializer.Serialize(step.Args);
        foreach (Match match in BindingRefRegex.Matches(argsJson))
        {
            var token = (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value).Trim();

            // Strip prefixes: "tool:shop.listProducts.items" -> "shop.listProducts"
            // or "step:step_1.field" -> "step_1"
            if (token.StartsWith("tool:", StringComparison.OrdinalIgnoreCase))
                token = token[5..];
            else if (token.StartsWith("step:", StringComparison.OrdinalIgnoreCase))
                token = token[5..];

            // The first segment(s) before any field path is the key
            // Tool names can contain dots (e.g. "shop.listProducts"), so we
            // extract just the reference key (the part before field access).
            // Since we can't know the exact tool name length, we add all
            // possible prefixes as dependencies.
            var parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 1; i <= parts.Length; i++)
            {
                deps.Add(string.Join('.', parts.Take(i)));
            }
        }

        return deps;
    }
}