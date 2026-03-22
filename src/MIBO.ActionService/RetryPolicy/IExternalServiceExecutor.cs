namespace MIBO.ActionService.RetryPolicy;

public interface IExternalServiceExecutor
{
    Task<object> ExecuteAsync(
        string handler,
        IReadOnlyDictionary<string, object?> args,
        Func<CancellationToken, Task<object>> operation,
        CancellationToken cancellationToken);

    Task ProcessRetryAsync(QueuedRetryMessage message, CancellationToken cancellationToken);
}
