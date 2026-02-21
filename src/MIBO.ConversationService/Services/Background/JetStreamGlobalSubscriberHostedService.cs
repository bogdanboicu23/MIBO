using System.Text;
using MIBO.ConversationService.DTOs.Eventing;
using Microsoft.Extensions.Options;
using NATS.Client;
using NATS.Client.JetStream;

namespace MIBO.ConversationService.Services.Background;

public interface IEventHandler
{
    Task HandleAsync(string subject, string json, CancellationToken ct);
}

public sealed class JetStreamGlobalSubscriberHostedService : BackgroundService
{
    private readonly IConnection _conn;
    private readonly IEventHandler _handler;
    private readonly NatsJetStreamOptions _opt;

    public JetStreamGlobalSubscriberHostedService(
        IConnection conn,
        IEventHandler handler,
        IOptions<NatsJetStreamOptions> opt)
    {
        _conn = conn;
        _handler = handler;
        _opt = opt.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var js = _conn.CreateJetStreamContext();

        var subOpts = PullSubscribeOptions.Builder()
            .WithStream(_opt.StreamName)
            .WithDurable(_opt.ConsumerDurableName)
            .Build();

        // Pull-based loop (control concurency & backpressure)
        var sub = js.PullSubscribe("", subOpts);

        _ = Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // fetch batch
                IList<Msg> msgs;
                try
                {
                    msgs = sub.Fetch(50, 1000); // 50 msgs, wait up to 1s
                }
                catch
                {
                    await Task.Delay(200, stoppingToken);
                    continue;
                }

                foreach (var m in msgs)
                {
                    try
                    {
                        var subject = m.Subject;
                        var json = Encoding.UTF8.GetString(m.Data);

                        await _handler.HandleAsync(subject, json, stoppingToken);
                        m.Ack();
                    }
                    catch
                    {
                        // no ack => retry later
                    }
                }
            }
        }, stoppingToken);

        return Task.CompletedTask;
    }
}