namespace MIBO.ActionService.RetryPolicy;

public sealed class NoOpExternalServiceExecutor : IExternalServiceExecutor
{
    public Task<object> ExecuteAsync(
        string handler,
        IReadOnlyDictionary<string, object?> args,
        Func<CancellationToken, Task<object>> operation,
        CancellationToken cancellationToken)
    {
        return operation(cancellationToken);
    }

    public Task ProcessRetryAsync(QueuedRetryMessage message, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
