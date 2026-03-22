using MIBO.Storage.Mongo.Integrations;
using Microsoft.Extensions.Options;

namespace MIBO.ActionService.RetryPolicy;

public sealed record ExternalServiceStatusItem(
    string ServiceKey,
    string ServiceName,
    string DisplayName,
    string BaseUrl,
    string Status,
    DateTime? LastCheckedAtUtc,
    DateTime? LastSuccessAtUtc,
    DateTime? LastFailureAtUtc,
    DateTime? OpenIncidentSinceUtc,
    int ConsecutiveFailures,
    long? LastDurationMs,
    string? LastErrorMessage,
    string? LastHandler,
    string? LastCorrelationId,
    string? LastOrigin,
    long TotalRequests,
    long TotalFailures
);

public sealed record ExternalServiceAuditItem(
    string ServiceKey,
    string ServiceName,
    string DisplayName,
    string Handler,
    string CorrelationId,
    string Origin,
    string Outcome,
    int Attempt,
    int LocalRetryCount,
    int MaxAttempts,
    long DurationMs,
    bool QueuedRetry,
    DateTime CreatedAtUtc,
    DateTime? NextRetryAtUtc,
    string? ErrorType,
    string? ErrorMessage
);

public sealed record ExternalServiceStatusCounts(
    int Total,
    int Monitoring,
    int Operational,
    int Degraded,
    int Outage
);

public sealed record ExternalServiceStatusSummary(
    bool Enabled,
    DateTime GeneratedAtUtc,
    string OverallStatus,
    ExternalServiceStatusCounts Counts,
    IReadOnlyList<ExternalServiceStatusItem> Services,
    IReadOnlyList<ExternalServiceAuditItem> Audits
);

public interface IExternalServiceStatusQuery
{
    Task<ExternalServiceStatusSummary> GetSummaryAsync(int auditLimit, CancellationToken cancellationToken);
}

public sealed class ExternalServiceStatusQuery(
    IExternalServiceRegistry registry,
    IExternalServiceMonitorStore monitorStore,
    IOptionsMonitor<RetryPolicyOptions> retryPolicyOptions)
    : IExternalServiceStatusQuery
{
    public async Task<ExternalServiceStatusSummary> GetSummaryAsync(int auditLimit, CancellationToken cancellationToken)
    {
        var options = retryPolicyOptions.CurrentValue;
        var enabled = options.AuditEnabled || options.StatusPageEnabled || (options.Enabled && options.UseRabbit);

        var knownServices = registry.GetKnownServices();
        var statusSnapshots = await monitorStore.ListStatusesAsync(cancellationToken);
        var snapshotMap = statusSnapshots.ToDictionary(snapshot => snapshot.ServiceKey, StringComparer.OrdinalIgnoreCase);
        var knownKeys = new HashSet<string>(knownServices.Select(service => service.ServiceKey), StringComparer.OrdinalIgnoreCase);

        var services = knownServices
            .Select(service =>
            {
                if (snapshotMap.TryGetValue(service.ServiceKey, out var snapshot))
                {
                    return MapSnapshot(snapshot);
                }

                return CreateMonitoringItem(service);
            })
            .Concat(statusSnapshots
                .Where(snapshot => !knownKeys.Contains(snapshot.ServiceKey))
                .Select(MapSnapshot))
            .OrderBy(GetServiceSortOrder)
            .ThenBy(service => service.DisplayName)
            .ToList();

        var audits = await monitorStore.ListAuditsAsync(auditLimit, null, null, cancellationToken);
        var auditItems = audits
            .Select(audit => new ExternalServiceAuditItem(
                audit.ServiceKey,
                audit.ServiceName,
                audit.DisplayName,
                audit.Handler,
                audit.CorrelationId,
                audit.Origin,
                audit.Outcome,
                audit.Attempt,
                audit.LocalRetryCount,
                audit.MaxAttempts,
                audit.DurationMs,
                audit.QueuedRetry,
                audit.CreatedAtUtc,
                audit.NextRetryAtUtc,
                audit.ErrorType,
                audit.ErrorMessage))
            .ToList();

        var counts = new ExternalServiceStatusCounts(
            services.Count,
            services.Count(service => service.Status == ExternalServiceStates.Monitoring),
            services.Count(service => service.Status == ExternalServiceStates.Operational),
            services.Count(service => service.Status == ExternalServiceStates.Degraded),
            services.Count(service => service.Status == ExternalServiceStates.Outage));

        var overallStatus = counts.Outage > 0
            ? ExternalServiceStates.Outage
            : counts.Degraded > 0
                ? ExternalServiceStates.Degraded
                : counts.Operational > 0
                    ? ExternalServiceStates.Operational
                    : ExternalServiceStates.Monitoring;

        return new ExternalServiceStatusSummary(
            enabled,
            DateTime.UtcNow,
            overallStatus,
            counts,
            services,
            auditItems);
    }

    private static ExternalServiceStatusItem MapSnapshot(ExternalServiceStatusSnapshot snapshot)
    {
        return new ExternalServiceStatusItem(
            snapshot.ServiceKey,
            snapshot.ServiceName,
            snapshot.DisplayName,
            snapshot.BaseUrl,
            snapshot.Status,
            snapshot.LastCheckedAtUtc,
            snapshot.LastSuccessAtUtc,
            snapshot.LastFailureAtUtc,
            snapshot.OpenIncidentSinceUtc,
            snapshot.ConsecutiveFailures,
            snapshot.LastDurationMs,
            snapshot.LastErrorMessage,
            snapshot.LastHandler,
            snapshot.LastCorrelationId,
            snapshot.LastOrigin,
            snapshot.TotalRequests,
            snapshot.TotalFailures);
    }

    private static ExternalServiceStatusItem CreateMonitoringItem(ExternalServiceDescriptor service)
    {
        return new ExternalServiceStatusItem(
            service.ServiceKey,
            service.ServiceName,
            service.DisplayName,
            service.BaseUrl,
            ExternalServiceStates.Monitoring,
            null,
            null,
            null,
            null,
            0,
            null,
            null,
            null,
            null,
            null,
            0,
            0);
    }

    private static int GetServiceSortOrder(ExternalServiceStatusItem service)
    {
        return service.ServiceKey switch
        {
            "platform" => 0,
            "ai-agent" => 1,
            _ => 10
        };
    }
}
