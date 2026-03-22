namespace MIBO.ActionService.RetryPolicy;

public sealed class NoOpRabbitMqRetryPublisher : IRabbitMqRetryPublisher
{
    public Task<bool> ScheduleRetryAsync(QueuedRetryMessage message, TimeSpan delay, CancellationToken cancellationToken)
        => Task.FromResult(false);

    public Task PublishDeadLetterAsync(QueuedRetryMessage message, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
