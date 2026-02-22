using MIBO.ConversationService.DTOs.Eventing;
using Microsoft.Extensions.Options;
using NATS.Client;
using NATS.Client.Internals;
using NATS.Client.JetStream;

namespace MIBO.ConversationService.Services.Background;

public sealed class JetStreamBootstrapHostedService : IHostedService
{
    private readonly IConnection _conn;
    private readonly NatsJetStreamOptions _opt;

    public JetStreamBootstrapHostedService(IConnection conn, IOptions<NatsJetStreamOptions> opt)
    {
        _conn = conn;
        _opt = opt.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var jsm = _conn.CreateJetStreamManagementContext();

        // Ensure stream exists
        try
        {
            jsm.GetStreamInfo(_opt.StreamName);
        }
        catch
        {
            var sc = StreamConfiguration.Builder()
                .WithName(_opt.StreamName)
                .WithSubjects(_opt.Subjects.ToArray())
                .WithStorageType(StorageType.File)     // persistent
                .WithRetentionPolicy(RetentionPolicy.Limits)
                .WithMaxAge(Duration.OfDays(7))
                .Build();

            jsm.AddStream(sc);
        }

        // Ensure durable consumer exists
        try
        {
            jsm.GetConsumerInfo(_opt.StreamName, _opt.ConsumerDurableName);
        }
        catch
        {
            var cc = ConsumerConfiguration.Builder()
                .WithDurable(_opt.ConsumerDurableName)
                .WithAckPolicy(AckPolicy.Explicit)
                .WithAckWait(Duration.OfSeconds(_opt.AckWaitSeconds))
                .WithMaxAckPending(_opt.MaxAckPending)
                .WithDeliverPolicy(DeliverPolicy.All) // replay from beginning (poți schimba în New)
                .Build();

            jsm.AddOrUpdateConsumer(_opt.StreamName, cc);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}