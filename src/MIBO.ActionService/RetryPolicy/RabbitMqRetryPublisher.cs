using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MIBO.ActionService.RetryPolicy;

public sealed class RabbitMqRetryPublisher(
    IOptionsMonitor<RetryPolicyOptions> retryPolicyOptions,
    IOptionsMonitor<RabbitMqOptions> rabbitMqOptions,
    ILogger<RabbitMqRetryPublisher> logger)
    : IRabbitMqRetryPublisher
{
    public Task<bool> ScheduleRetryAsync(QueuedRetryMessage message, TimeSpan delay, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var retryOptions = retryPolicyOptions.CurrentValue;
        if (!retryOptions.Enabled || !retryOptions.UseRabbit)
        {
            return Task.FromResult(false);
        }

        try
        {
            var options = rabbitMqOptions.CurrentValue;
            using var connection = options.CreateConnectionFactory().CreateConnection();
            using var channel = connection.CreateModel();
            EnsureTopology(channel, options);

            var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Expiration = Math.Max(1, (long)delay.TotalMilliseconds).ToString(CultureInfo.InvariantCulture);

            channel.BasicPublish(
                exchange: options.RetryExchange,
                routingKey: "retry",
                mandatory: false,
                basicProperties: properties,
                body: payload);

            return Task.FromResult(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to schedule RabbitMQ retry for handler {Handler}.", message.Handler);
            return Task.FromResult(false);
        }
    }

    public Task PublishDeadLetterAsync(QueuedRetryMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var retryOptions = retryPolicyOptions.CurrentValue;
        if (!retryOptions.Enabled || !retryOptions.UseRabbit)
        {
            return Task.CompletedTask;
        }

        try
        {
            var options = rabbitMqOptions.CurrentValue;
            using var connection = options.CreateConnectionFactory().CreateConnection();
            using var channel = connection.CreateModel();
            EnsureTopology(channel, options);

            var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";

            channel.BasicPublish(
                exchange: options.DeadLetterExchange,
                routingKey: "deadletter",
                mandatory: false,
                basicProperties: properties,
                body: payload);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to publish dead-letter message for handler {Handler}.", message.Handler);
        }

        return Task.CompletedTask;
    }

    internal static void EnsureTopology(IModel channel, RabbitMqOptions options)
    {
        channel.ExchangeDeclare(options.ExecuteExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.ExchangeDeclare(options.RetryExchange, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.ExchangeDeclare(options.DeadLetterExchange, ExchangeType.Direct, durable: true, autoDelete: false);

        channel.QueueDeclare(options.ExecuteQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueDeclare(
            options.RetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = options.ExecuteExchange,
                ["x-dead-letter-routing-key"] = "execute",
            });
        channel.QueueDeclare(options.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false);

        channel.QueueBind(options.ExecuteQueue, options.ExecuteExchange, "execute");
        channel.QueueBind(options.RetryQueue, options.RetryExchange, "retry");
        channel.QueueBind(options.DeadLetterQueue, options.DeadLetterExchange, "deadletter");
    }
}
