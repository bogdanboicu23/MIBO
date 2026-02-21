using System.Text;
using MIBO.ConversationService.DTOs.Eventing;
using MIBO.ConversationService.Services.Eventing;
using Microsoft.Extensions.Options;
using NATS.Client;

namespace MIBO.ConversationService.Services.Background;

public sealed class NatsGlobalSubscriberHostedService : BackgroundService
{
    private readonly IConnection _conn;
    private readonly EventRefreshConsumer _consumer;
    private readonly EventSubscriberOptions _opt;

    public NatsGlobalSubscriberHostedService(IConnection conn, EventRefreshConsumer consumer, IOptions<EventSubscriberOptions> opt)
    {
        _conn = conn;
        _consumer = consumer;
        _opt = opt.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var subj in _opt.Subjects)
        {
            var sub = _conn.SubscribeAsync(subj);
            sub.MessageHandler += async (_, args) =>
            {
                var subject = args.Message.Subject; // IMPORTANT: actual subject that matched pattern
                var json = Encoding.UTF8.GetString(args.Message.Data);
                await _consumer.HandleAsync(subject, json, stoppingToken);
            };
            sub.Start();
        }

        return Task.CompletedTask;
    }
}