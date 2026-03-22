using MIBO.Storage.Mongo.Integrations;

namespace MIBO.ConversationService.Monitoring;

public interface IPlatformActivityMonitor
{
    Task RecordPlatformSuccessAsync(string correlationId, string handler, long durationMs, CancellationToken ct);
    Task RecordPlatformFailureAsync(string correlationId, string handler, long durationMs, string? errorMessage, CancellationToken ct);
    Task RecordAiAgentSuccessAsync(string correlationId, string handler, long durationMs, CancellationToken ct);
    Task RecordAiAgentFailureAsync(string correlationId, string handler, long durationMs, string? errorMessage, CancellationToken ct);
}

public sealed class PlatformActivityMonitor(
    IExternalServiceMonitorStore monitorStore,
    IConfiguration configuration,
    ILogger<PlatformActivityMonitor> logger)
    : IPlatformActivityMonitor
{
    private const string InteractiveOrigin = "interactive";

    private static readonly SyntheticComponent PlatformComponent = new(
        "platform",
        "MIBO.Platform",
        "MIBO Platform",
        "conversation-service / api-gateway");

    public Task RecordPlatformSuccessAsync(string correlationId, string handler, long durationMs, CancellationToken ct)
        => RecordAsync(PlatformComponent, ExternalServiceOutcomes.Succeeded, correlationId, handler, durationMs, null, ct);

    public Task RecordPlatformFailureAsync(string correlationId, string handler, long durationMs, string? errorMessage, CancellationToken ct)
        => RecordAsync(PlatformComponent, ExternalServiceOutcomes.Failed, correlationId, handler, durationMs, errorMessage, ct);

    public Task RecordAiAgentSuccessAsync(string correlationId, string handler, long durationMs, CancellationToken ct)
        => RecordAsync(GetAiAgentComponent(), ExternalServiceOutcomes.Succeeded, correlationId, handler, durationMs, null, ct);

    public Task RecordAiAgentFailureAsync(string correlationId, string handler, long durationMs, string? errorMessage, CancellationToken ct)
        => RecordAsync(GetAiAgentComponent(), ExternalServiceOutcomes.Failed, correlationId, handler, durationMs, errorMessage, ct);

    private SyntheticComponent GetAiAgentComponent()
    {
        return new SyntheticComponent(
            "ai-agent",
            "MIBO.Agent",
            "AI Agent",
            ResolveBaseUrl(
                configuration,
                "http://localhost:8088",
                "AGENT_SERVICE_URL",
                "Planner:BaseUrl",
                "services:langchain-service:http:0"));
    }

    private async Task RecordAsync(
        SyntheticComponent component,
        string outcome,
        string correlationId,
        string handler,
        long durationMs,
        string? errorMessage,
        CancellationToken ct)
    {
        try
        {
            var now = DateTime.UtcNow;
            var previous = await monitorStore.GetStatusAsync(component.ServiceKey, ct);
            var succeeded = outcome == ExternalServiceOutcomes.Succeeded;
            var previousOpenIncidentSince = previous?.OpenIncidentSinceUtc;
            DateTime? nextOpenIncidentSince = succeeded ? null : previousOpenIncidentSince ?? now;
            var nextConsecutiveFailures = succeeded ? 0 : (previous?.ConsecutiveFailures ?? 0) + 1;
            var nextTotalRequests = (previous?.TotalRequests ?? 0) + 1;
            var nextTotalFailures = (previous?.TotalFailures ?? 0) + (succeeded ? 0 : 1);

            await monitorStore.AppendAuditAsync(
                new ExternalServiceAuditRecord(
                    component.ServiceKey,
                    component.ServiceName,
                    component.DisplayName,
                    handler,
                    correlationId,
                    InteractiveOrigin,
                    outcome,
                    1,
                    0,
                    1,
                    durationMs,
                    false,
                    now,
                    null,
                    succeeded ? null : "PlatformRequestFailure",
                    succeeded ? null : TrimError(errorMessage)),
                ct);

            await monitorStore.UpsertStatusAsync(
                new ExternalServiceStatusSnapshot(
                    component.ServiceKey,
                    component.ServiceName,
                    component.DisplayName,
                    component.BaseUrl,
                    succeeded ? ExternalServiceStates.Operational : ExternalServiceStates.Outage,
                    now,
                    succeeded ? now : previous?.LastSuccessAtUtc,
                    succeeded ? previous?.LastFailureAtUtc : now,
                    nextOpenIncidentSince,
                    nextConsecutiveFailures,
                    durationMs,
                    succeeded ? null : TrimError(errorMessage),
                    handler,
                    correlationId,
                    InteractiveOrigin,
                    nextTotalRequests,
                    nextTotalFailures),
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record platform activity for component {ComponentKey}.", component.ServiceKey);
        }
    }

    private static string ResolveBaseUrl(IConfiguration configuration, string defaultUrl, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return defaultUrl;
    }

    private static string? TrimError(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 300 ? trimmed : trimmed[..300];
    }

    private sealed record SyntheticComponent(
        string ServiceKey,
        string ServiceName,
        string DisplayName,
        string BaseUrl);
}
