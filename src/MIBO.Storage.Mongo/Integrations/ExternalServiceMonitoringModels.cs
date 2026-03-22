using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MIBO.Storage.Mongo.Integrations;

public static class ExternalServiceOutcomes
{
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string FailedQueued = "failed_queued";
}

public static class ExternalServiceStates
{
    public const string Monitoring = "monitoring";
    public const string Operational = "operational";
    public const string Degraded = "degraded";
    public const string Outage = "outage";
}

public sealed record ExternalServiceAuditRecord(
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

public sealed record ExternalServiceStatusSnapshot(
    string ServiceKey,
    string ServiceName,
    string DisplayName,
    string BaseUrl,
    string Status,
    DateTime LastCheckedAtUtc,
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

[BsonIgnoreExtraElements]
public sealed class ExternalServiceAuditDoc
{
    [BsonId] public ObjectId Id { get; set; }

    [BsonElement("serviceKey")] public string ServiceKey { get; set; } = default!;
    [BsonElement("serviceName")] public string ServiceName { get; set; } = default!;
    [BsonElement("displayName")] public string DisplayName { get; set; } = default!;
    [BsonElement("handler")] public string Handler { get; set; } = default!;
    [BsonElement("correlationId")] public string CorrelationId { get; set; } = default!;
    [BsonElement("origin")] public string Origin { get; set; } = default!;
    [BsonElement("outcome")] public string Outcome { get; set; } = default!;
    [BsonElement("attempt")] public int Attempt { get; set; }
    [BsonElement("localRetryCount")] public int LocalRetryCount { get; set; }
    [BsonElement("maxAttempts")] public int MaxAttempts { get; set; }
    [BsonElement("durationMs")] public long DurationMs { get; set; }
    [BsonElement("queuedRetry")] public bool QueuedRetry { get; set; }
    [BsonElement("createdAtUtc")] public DateTime CreatedAtUtc { get; set; }
    [BsonElement("nextRetryAtUtc")] public DateTime? NextRetryAtUtc { get; set; }
    [BsonElement("errorType")] public string? ErrorType { get; set; }
    [BsonElement("errorMessage")] public string? ErrorMessage { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class ExternalServiceStatusDoc
{
    [BsonId] public string ServiceKey { get; set; } = default!;

    [BsonElement("serviceName")] public string ServiceName { get; set; } = default!;
    [BsonElement("displayName")] public string DisplayName { get; set; } = default!;
    [BsonElement("baseUrl")] public string BaseUrl { get; set; } = default!;
    [BsonElement("status")] public string Status { get; set; } = ExternalServiceStates.Operational;
    [BsonElement("lastCheckedAtUtc")] public DateTime LastCheckedAtUtc { get; set; }
    [BsonElement("lastSuccessAtUtc")] public DateTime? LastSuccessAtUtc { get; set; }
    [BsonElement("lastFailureAtUtc")] public DateTime? LastFailureAtUtc { get; set; }
    [BsonElement("openIncidentSinceUtc")] public DateTime? OpenIncidentSinceUtc { get; set; }
    [BsonElement("consecutiveFailures")] public int ConsecutiveFailures { get; set; }
    [BsonElement("lastDurationMs")] public long? LastDurationMs { get; set; }
    [BsonElement("lastErrorMessage")] public string? LastErrorMessage { get; set; }
    [BsonElement("lastHandler")] public string? LastHandler { get; set; }
    [BsonElement("lastCorrelationId")] public string? LastCorrelationId { get; set; }
    [BsonElement("lastOrigin")] public string? LastOrigin { get; set; }
    [BsonElement("totalRequests")] public long TotalRequests { get; set; }
    [BsonElement("totalFailures")] public long TotalFailures { get; set; }
    [BsonElement("updatedAtUtc")] public DateTime UpdatedAtUtc { get; set; }
}
