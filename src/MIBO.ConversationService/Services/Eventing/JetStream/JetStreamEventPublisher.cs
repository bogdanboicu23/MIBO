// /backend/src/Infrastructure/Eventing/JetStreamEventPublisher.cs

using System.Text;
using System.Text.Json;
using MIBO.ConversationService.DTOs.Eventing;
using NATS.Client;
using NATS.Client.JetStream;

namespace MIBO.ConversationService.Services.Eventing.JetStream;

public interface IEventPublisher
{
    Task PublishAsync(string subject, EventEnvelopeV1 evt, CancellationToken ct);
}

public sealed class JetStreamEventPublisher : IEventPublisher
{
    private readonly IJetStream _js;

    public JetStreamEventPublisher(IConnection conn) => _js = conn.CreateJetStreamContext();

    public Task PublishAsync(string subject, EventEnvelopeV1 evt, CancellationToken ct)
    {
        evt.Subject = subject;

        var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var data = Encoding.UTF8.GetBytes(json);

        _js.Publish(subject, data);
        return Task.CompletedTask;
    }
}