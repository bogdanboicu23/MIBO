using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MIBO.ActionService.RetryPolicy;

public sealed class RabbitMqRetryConsumerHostedService(
    IExternalServiceExecutor executor,
    IOptionsMonitor<RetryPolicyOptions> retryPolicyOptions,
    IOptionsMonitor<RabbitMqOptions> rabbitMqOptions,
    ILogger<RabbitMqRetryConsumerHostedService> logger)
    : BackgroundService
{
    private IConnection? _connection;
    private IModel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retryOptions = retryPolicyOptions.CurrentValue;
        if (!retryOptions.Enabled || !retryOptions.UseRabbit)
        {
            return;
        }

        try
        {
            var options = rabbitMqOptions.CurrentValue;
            _connection = options.CreateConnectionFactory().CreateConnection();
            _channel = _connection.CreateModel();
            RabbitMqRetryPublisher.EnsureTopology(_channel, options);
            _channel.BasicQos(0, (ushort)Math.Clamp(retryOptions.DispatcherBatchSize, 1, 100), false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, eventArgs) =>
            {
                await HandleMessageAsync(eventArgs, stoppingToken);
            };

            _channel.BasicConsume(
                queue: options.ExecuteQueue,
                autoAck: false,
                consumer: consumer);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RabbitMQ retry consumer stopped unexpectedly.");
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var payload = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var message = JsonSerializer.Deserialize<QueuedRetryMessage>(payload);
            if (message is null)
            {
                logger.LogWarning("RabbitMQ retry consumer received an empty or invalid message.");
                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                return;
            }

            await executor.ProcessRetryAsync(message, cancellationToken);
            _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "RabbitMQ retry consumer failed while processing a retry message.");
            _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
    }
}
