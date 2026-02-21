
using MIBO.ConversationService.DTOs.Eventing;
using MIBO.ConversationService.Middleware.Http;

namespace MIBO.ConversationService.Services.Eventing.Factory;

public sealed class DefaultEventEnvelopeFactory : IEventEnvelopeFactory
{
    private readonly IHeaderContextAccessor _headers;

    public DefaultEventEnvelopeFactory(IHeaderContextAccessor headers) => _headers = headers;

    public EventEnvelopeV1 CreateBase(string subject, string conversationId, string userId)
    {
        var h = _headers.Get();

        return new EventEnvelopeV1
        {
            Schema = "event.v1",
            Subject = subject,
            EventId = Guid.NewGuid().ToString("N"),
            CorrelationId = h.CorrelationId,
            ConversationId = conversationId,
            UserId = userId,
            OccurredAtUtc = DateTime.UtcNow
        };
    }
}