namespace MIBO.Storage.Mongo.Integrations;

public interface IExternalServiceMonitorStore
{
    Task AppendAuditAsync(ExternalServiceAuditRecord record, CancellationToken ct);
    Task<ExternalServiceStatusSnapshot?> GetStatusAsync(string serviceKey, CancellationToken ct);
    Task UpsertStatusAsync(ExternalServiceStatusSnapshot snapshot, CancellationToken ct);
    Task<IReadOnlyList<ExternalServiceStatusSnapshot>> ListStatusesAsync(CancellationToken ct);
    Task<IReadOnlyList<ExternalServiceAuditRecord>> ListAuditsAsync(int limit, string? serviceKey, string? outcome, CancellationToken ct);
}
