using MIBO.Storage.Mongo.Integrations;

namespace MIBO.ActionService.RetryPolicy;

public sealed class NoOpExternalServiceMonitorStore : IExternalServiceMonitorStore
{
    public Task AppendAuditAsync(ExternalServiceAuditRecord record, CancellationToken ct) => Task.CompletedTask;

    public Task<ExternalServiceStatusSnapshot?> GetStatusAsync(string serviceKey, CancellationToken ct)
        => Task.FromResult<ExternalServiceStatusSnapshot?>(null);

    public Task UpsertStatusAsync(ExternalServiceStatusSnapshot snapshot, CancellationToken ct) => Task.CompletedTask;

    public Task<IReadOnlyList<ExternalServiceStatusSnapshot>> ListStatusesAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ExternalServiceStatusSnapshot>>([]);

    public Task<IReadOnlyList<ExternalServiceAuditRecord>> ListAuditsAsync(int limit, string? serviceKey, string? outcome, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ExternalServiceAuditRecord>>([]);
}
