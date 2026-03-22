using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MIBO.Storage.Mongo.Integrations;

public sealed class MongoExternalServiceMonitorStore : IExternalServiceMonitorStore
{
    private readonly IMongoCollection<ExternalServiceAuditDoc> _audits;
    private readonly IMongoCollection<ExternalServiceStatusDoc> _statuses;

    public MongoExternalServiceMonitorStore(IMongoDatabase db, IOptions<MongoOptions> options)
    {
        _audits = db.GetCollection<ExternalServiceAuditDoc>(options.Value.ExternalServiceAuditsCollection);
        _statuses = db.GetCollection<ExternalServiceStatusDoc>(options.Value.ExternalServiceStatusesCollection);
    }

    public Task AppendAuditAsync(ExternalServiceAuditRecord record, CancellationToken ct)
    {
        var doc = new ExternalServiceAuditDoc
        {
            ServiceKey = record.ServiceKey,
            ServiceName = record.ServiceName,
            DisplayName = record.DisplayName,
            Handler = record.Handler,
            CorrelationId = record.CorrelationId,
            Origin = record.Origin,
            Outcome = record.Outcome,
            Attempt = record.Attempt,
            LocalRetryCount = record.LocalRetryCount,
            MaxAttempts = record.MaxAttempts,
            DurationMs = record.DurationMs,
            QueuedRetry = record.QueuedRetry,
            CreatedAtUtc = record.CreatedAtUtc,
            NextRetryAtUtc = record.NextRetryAtUtc,
            ErrorType = record.ErrorType,
            ErrorMessage = record.ErrorMessage,
        };

        return _audits.InsertOneAsync(doc, cancellationToken: ct);
    }

    public Task UpsertStatusAsync(ExternalServiceStatusSnapshot snapshot, CancellationToken ct)
    {
        var filter = Builders<ExternalServiceStatusDoc>.Filter.Eq(x => x.ServiceKey, snapshot.ServiceKey);
        var doc = new ExternalServiceStatusDoc
        {
            ServiceKey = snapshot.ServiceKey,
            ServiceName = snapshot.ServiceName,
            DisplayName = snapshot.DisplayName,
            BaseUrl = snapshot.BaseUrl,
            Status = snapshot.Status,
            LastCheckedAtUtc = snapshot.LastCheckedAtUtc,
            LastSuccessAtUtc = snapshot.LastSuccessAtUtc,
            LastFailureAtUtc = snapshot.LastFailureAtUtc,
            OpenIncidentSinceUtc = snapshot.OpenIncidentSinceUtc,
            ConsecutiveFailures = snapshot.ConsecutiveFailures,
            LastDurationMs = snapshot.LastDurationMs,
            LastErrorMessage = snapshot.LastErrorMessage,
            LastHandler = snapshot.LastHandler,
            LastCorrelationId = snapshot.LastCorrelationId,
            LastOrigin = snapshot.LastOrigin,
            TotalRequests = snapshot.TotalRequests,
            TotalFailures = snapshot.TotalFailures,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        return _statuses.ReplaceOneAsync(
            filter,
            doc,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }

    public async Task<ExternalServiceStatusSnapshot?> GetStatusAsync(string serviceKey, CancellationToken ct)
    {
        var doc = await _statuses
            .Find(Builders<ExternalServiceStatusDoc>.Filter.Eq(x => x.ServiceKey, serviceKey))
            .FirstOrDefaultAsync(ct);

        return doc is null ? null : MapStatus(doc);
    }

    public async Task<IReadOnlyList<ExternalServiceStatusSnapshot>> ListStatusesAsync(CancellationToken ct)
    {
        var list = await _statuses
            .Find(Builders<ExternalServiceStatusDoc>.Filter.Empty)
            .SortBy(x => x.DisplayName)
            .ToListAsync(ct);

        return list.Select(MapStatus).ToList();
    }

    public async Task<IReadOnlyList<ExternalServiceAuditRecord>> ListAuditsAsync(
        int limit,
        string? serviceKey,
        string? outcome,
        CancellationToken ct)
    {
        var boundedLimit = Math.Clamp(limit, 1, 1000);
        var filter = Builders<ExternalServiceAuditDoc>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(serviceKey))
        {
            filter &= Builders<ExternalServiceAuditDoc>.Filter.Eq(x => x.ServiceKey, serviceKey);
        }

        if (!string.IsNullOrWhiteSpace(outcome))
        {
            filter &= Builders<ExternalServiceAuditDoc>.Filter.Eq(x => x.Outcome, outcome);
        }

        var list = await _audits
            .Find(filter)
            .SortByDescending(x => x.CreatedAtUtc)
            .Limit(boundedLimit)
            .ToListAsync(ct);

        return list.Select(MapAudit).ToList();
    }

    private static ExternalServiceStatusSnapshot MapStatus(ExternalServiceStatusDoc doc)
    {
        return new ExternalServiceStatusSnapshot(
            doc.ServiceKey,
            doc.ServiceName,
            doc.DisplayName,
            doc.BaseUrl,
            doc.Status,
            doc.LastCheckedAtUtc,
            doc.LastSuccessAtUtc,
            doc.LastFailureAtUtc,
            doc.OpenIncidentSinceUtc,
            doc.ConsecutiveFailures,
            doc.LastDurationMs,
            doc.LastErrorMessage,
            doc.LastHandler,
            doc.LastCorrelationId,
            doc.LastOrigin,
            doc.TotalRequests,
            doc.TotalFailures);
    }

    private static ExternalServiceAuditRecord MapAudit(ExternalServiceAuditDoc doc)
    {
        return new ExternalServiceAuditRecord(
            doc.ServiceKey,
            doc.ServiceName,
            doc.DisplayName,
            doc.Handler,
            doc.CorrelationId,
            doc.Origin,
            doc.Outcome,
            doc.Attempt,
            doc.LocalRetryCount,
            doc.MaxAttempts,
            doc.DurationMs,
            doc.QueuedRetry,
            doc.CreatedAtUtc,
            doc.NextRetryAtUtc,
            doc.ErrorType,
            doc.ErrorMessage);
    }
}
