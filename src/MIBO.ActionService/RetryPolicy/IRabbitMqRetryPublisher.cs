namespace MIBO.ActionService.RetryPolicy;

public interface IRabbitMqRetryPublisher
{
    Task<bool> ScheduleRetryAsync(QueuedRetryMessage message, TimeSpan delay, CancellationToken cancellationToken);
    Task PublishDeadLetterAsync(QueuedRetryMessage message, CancellationToken cancellationToken);
}
