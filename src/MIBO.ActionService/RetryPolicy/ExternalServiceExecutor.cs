using System.Diagnostics;
using System.Runtime.ExceptionServices;
using MIBO.ActionService.ExternalServices.Abstractions;
using MIBO.Storage.Mongo.Integrations;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;

namespace MIBO.ActionService.RetryPolicy;

public sealed class ExternalServiceExecutor(
    IEnumerable<IExternalDataSourceHandler> handlers,
    IExternalServiceRegistry registry,
    IExternalServiceMonitorStore monitorStore,
    IRabbitMqRetryPublisher retryPublisher,
    IOptionsMonitor<RetryPolicyOptions> retryPolicyOptions,
    ILogger<ExternalServiceExecutor> logger)
    : IExternalServiceExecutor
{
    private readonly IReadOnlyList<IExternalDataSourceHandler> _handlers = handlers.ToList();

    public async Task<object> ExecuteAsync(
        string handler,
        IReadOnlyDictionary<string, object?> args,
        Func<CancellationToken, Task<object>> operation,
        CancellationToken cancellationToken)
    {
        var descriptor = registry.Resolve(handler);
        if (descriptor is null)
        {
            return await operation(cancellationToken);
        }

        var correlationId = Guid.NewGuid().ToString("N");
        var result = await TryExecuteOperationAsync(operation, cancellationToken);
        if (result.Succeeded)
        {
            await PersistSuccessAsync(
                descriptor,
                handler,
                correlationId,
                ExternalServiceExecutionOrigins.Interactive,
                attempt: 1,
                maxAttempts: retryPolicyOptions.CurrentValue.MaxAttempts,
                result,
                cancellationToken);

            return result.Value!;
        }

        var error = result.Error!;
        var queuedRetry = await ScheduleNextAttemptIfNeededAsync(
            descriptor,
            handler,
            args,
            correlationId,
            attempt: 1,
            cancellationToken);

        await PersistFailureAsync(
            descriptor,
            handler,
            correlationId,
            ExternalServiceExecutionOrigins.Interactive,
            attempt: 1,
            maxAttempts: retryPolicyOptions.CurrentValue.MaxAttempts,
            result,
            queuedRetry,
            cancellationToken);

        ExceptionDispatchInfo.Capture(error).Throw();
        return default!;
    }

    public async Task ProcessRetryAsync(QueuedRetryMessage message, CancellationToken cancellationToken)
    {
        var descriptor = registry.Resolve(message.Handler);
        var externalHandler = _handlers.FirstOrDefault(candidate => candidate.CanHandle(message.Handler));
        if (descriptor is null || externalHandler is null)
        {
            logger.LogWarning("Retry message dropped because handler {Handler} is no longer registered.", message.Handler);
            return;
        }

        var args = message.ToArguments();
        var result = await TryExecuteOperationAsync(
            token => externalHandler.QueryAsync(message.Handler, args, token),
            cancellationToken);

        if (result.Succeeded)
        {
            await PersistSuccessAsync(
                descriptor,
                message.Handler,
                message.CorrelationId,
                ExternalServiceExecutionOrigins.BackgroundRetry,
                message.Attempt,
                message.MaxAttempts,
                result,
                cancellationToken);

            return;
        }

        var queuedRetry = await ScheduleNextAttemptIfNeededAsync(
            descriptor,
            message.Handler,
            args,
            message.CorrelationId,
            message.Attempt,
            cancellationToken,
            message.MaxAttempts);

        await PersistFailureAsync(
            descriptor,
            message.Handler,
            message.CorrelationId,
            ExternalServiceExecutionOrigins.BackgroundRetry,
            message.Attempt,
            message.MaxAttempts,
            result,
            queuedRetry,
            cancellationToken);

        if (!queuedRetry.WasQueued)
        {
            await retryPublisher.PublishDeadLetterAsync(message, cancellationToken);
        }
    }

    private async Task<ExecutionAttemptResult> TryExecuteOperationAsync(
        Func<CancellationToken, Task<object>> operation,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var localRetryCount = 0;

        try
        {
            var options = retryPolicyOptions.CurrentValue;
            object value;

            if (options.Enabled)
            {
                var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(Math.Max(1, options.LocalTimeoutSeconds)));
                var retryPolicy = Policy
                    .Handle<Exception>(ShouldRetry)
                    .WaitAndRetryAsync(
                        Math.Max(0, options.LocalRetryCount),
                        retryAttempt => TimeSpan.FromMilliseconds(150 * retryAttempt),
                        onRetry: (_, _, retryCount, _) => localRetryCount = retryCount);

                var wrappedPolicy = Policy.WrapAsync(retryPolicy, timeoutPolicy);
                value = await wrappedPolicy.ExecuteAsync(ct => operation(ct), cancellationToken);
            }
            else
            {
                value = await operation(cancellationToken);
            }

            stopwatch.Stop();
            return ExecutionAttemptResult.Success(value, localRetryCount, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            return ExecutionAttemptResult.Failure(ex, localRetryCount, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<QueuedRetryDecision> ScheduleNextAttemptIfNeededAsync(
        ExternalServiceDescriptor descriptor,
        string handler,
        IReadOnlyDictionary<string, object?> args,
        string correlationId,
        int attempt,
        CancellationToken cancellationToken,
        int? maxAttemptsOverride = null)
    {
        var options = retryPolicyOptions.CurrentValue;
        var maxAttempts = maxAttemptsOverride ?? options.MaxAttempts;
        if (!options.Enabled || !options.UseRabbit || !options.IsServiceMigrated(descriptor.ServiceName) || attempt >= maxAttempts)
        {
            return QueuedRetryDecision.NotQueued;
        }

        var nextAttempt = attempt + 1;
        var delay = ComputeRetryDelay(nextAttempt, options);
        var nextRetryAtUtc = DateTime.UtcNow.Add(delay);
        var message = new QueuedRetryMessage
        {
            ServiceKey = descriptor.ServiceKey,
            Handler = handler,
            CorrelationId = correlationId,
            ArgsJson = QueuedRetryMessage.SerializeArguments(args),
            Attempt = nextAttempt,
            MaxAttempts = maxAttempts,
            InitialOrigin = ExternalServiceExecutionOrigins.Interactive,
            EnqueuedAtUtc = DateTime.UtcNow,
            NextAttemptAtUtc = nextRetryAtUtc,
        };

        var wasQueued = await retryPublisher.ScheduleRetryAsync(message, delay, cancellationToken);
        return wasQueued
            ? new QueuedRetryDecision(true, nextRetryAtUtc)
            : QueuedRetryDecision.NotQueued;
    }

    private async Task PersistSuccessAsync(
        ExternalServiceDescriptor descriptor,
        string handler,
        string correlationId,
        string origin,
        int attempt,
        int maxAttempts,
        ExecutionAttemptResult result,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await monitorStore.AppendAuditAsync(
            new ExternalServiceAuditRecord(
                descriptor.ServiceKey,
                descriptor.ServiceName,
                descriptor.DisplayName,
                handler,
                correlationId,
                origin,
                ExternalServiceOutcomes.Succeeded,
                attempt,
                result.LocalRetryCount,
                maxAttempts,
                result.DurationMs,
                false,
                now,
                null,
                null,
                null),
            cancellationToken);

        var previous = await monitorStore.GetStatusAsync(descriptor.ServiceKey, cancellationToken);
        await monitorStore.UpsertStatusAsync(
            new ExternalServiceStatusSnapshot(
                descriptor.ServiceKey,
                descriptor.ServiceName,
                descriptor.DisplayName,
                descriptor.BaseUrl,
                ExternalServiceStates.Operational,
                now,
                now,
                previous?.LastFailureAtUtc,
                null,
                0,
                result.DurationMs,
                null,
                handler,
                correlationId,
                origin,
                (previous?.TotalRequests ?? 0) + 1,
                previous?.TotalFailures ?? 0),
            cancellationToken);
    }

    private async Task PersistFailureAsync(
        ExternalServiceDescriptor descriptor,
        string handler,
        string correlationId,
        string origin,
        int attempt,
        int maxAttempts,
        ExecutionAttemptResult result,
        QueuedRetryDecision queuedRetry,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var outcome = queuedRetry.WasQueued
            ? ExternalServiceOutcomes.FailedQueued
            : ExternalServiceOutcomes.Failed;

        await monitorStore.AppendAuditAsync(
            new ExternalServiceAuditRecord(
                descriptor.ServiceKey,
                descriptor.ServiceName,
                descriptor.DisplayName,
                handler,
                correlationId,
                origin,
                outcome,
                attempt,
                result.LocalRetryCount,
                maxAttempts,
                result.DurationMs,
                queuedRetry.WasQueued,
                now,
                queuedRetry.NextRetryAtUtc,
                result.Error?.GetType().Name,
                result.Error?.Message),
            cancellationToken);

        var previous = await monitorStore.GetStatusAsync(descriptor.ServiceKey, cancellationToken);
        var consecutiveFailures = (previous?.ConsecutiveFailures ?? 0) + 1;
        var status = consecutiveFailures >= 2 || (!queuedRetry.WasQueued && attempt >= maxAttempts)
            ? ExternalServiceStates.Outage
            : ExternalServiceStates.Degraded;

        await monitorStore.UpsertStatusAsync(
            new ExternalServiceStatusSnapshot(
                descriptor.ServiceKey,
                descriptor.ServiceName,
                descriptor.DisplayName,
                descriptor.BaseUrl,
                status,
                now,
                previous?.LastSuccessAtUtc,
                now,
                previous?.OpenIncidentSinceUtc ?? now,
                consecutiveFailures,
                result.DurationMs,
                result.Error?.Message,
                handler,
                correlationId,
                origin,
                (previous?.TotalRequests ?? 0) + 1,
                (previous?.TotalFailures ?? 0) + 1),
            cancellationToken);
    }

    private static TimeSpan ComputeRetryDelay(int attempt, RetryPolicyOptions options)
    {
        var exponent = Math.Max(0, attempt - 2);
        var delaySeconds = options.InitialRetryDelaySeconds * Math.Pow(2, exponent);
        var boundedSeconds = Math.Min(delaySeconds, options.MaxRetryDelaySeconds);
        return TimeSpan.FromSeconds(Math.Max(1, boundedSeconds));
    }

    private static bool ShouldRetry(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => false,
            TimeoutRejectedException => true,
            HttpRequestException httpRequestException when httpRequestException.StatusCode is null => true,
            HttpRequestException httpRequestException when httpRequestException.StatusCode is System.Net.HttpStatusCode.RequestTimeout => true,
            HttpRequestException httpRequestException when httpRequestException.StatusCode is System.Net.HttpStatusCode.TooManyRequests => true,
            HttpRequestException httpRequestException when (int)httpRequestException.StatusCode! >= 500 => true,
            _ => false,
        };
    }

    private sealed record ExecutionAttemptResult(
        bool Succeeded,
        object? Value,
        Exception? Error,
        int LocalRetryCount,
        long DurationMs)
    {
        public static ExecutionAttemptResult Success(object value, int localRetryCount, long durationMs)
            => new(true, value, null, localRetryCount, durationMs);

        public static ExecutionAttemptResult Failure(Exception error, int localRetryCount, long durationMs)
            => new(false, null, error, localRetryCount, durationMs);
    }

    private sealed record QueuedRetryDecision(bool WasQueued, DateTime? NextRetryAtUtc)
    {
        public static QueuedRetryDecision NotQueued { get; } = new(false, null);
    }
}
