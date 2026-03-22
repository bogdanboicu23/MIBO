namespace MIBO.ActionService.RetryPolicy;

public sealed class RetryPolicyOptions
{
    public const string SectionName = "RetryPolicy";

    public bool Enabled { get; set; }
    public bool UseRabbit { get; set; }
    public bool AuditEnabled { get; set; }
    public bool StatusPageEnabled { get; set; }
    public int MaxAttempts { get; set; } = 4;
    public int LocalRetryCount { get; set; } = 2;
    public int LocalTimeoutSeconds { get; set; } = 5;
    public int InitialRetryDelaySeconds { get; set; } = 30;
    public int MaxRetryDelaySeconds { get; set; } = 300;
    public int RetryDispatcherIntervalSeconds { get; set; } = 15;
    public int DispatcherBatchSize { get; set; } = 25;
    public int LockTimeoutSeconds { get; set; } = 120;
    public int StatusLookbackMinutes { get; set; } = 180;
    public string[] MigratedServices { get; set; } = [];

    public bool IsServiceMigrated(string serviceName)
    {
        return MigratedServices.Any(candidate =>
            string.Equals(candidate, serviceName, StringComparison.OrdinalIgnoreCase));
    }
}
