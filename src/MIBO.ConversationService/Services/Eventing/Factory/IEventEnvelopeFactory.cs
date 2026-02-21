using MIBO.ConversationService.DTOs.Eventing;

namespace MIBO.ConversationService.Services.Eventing.Factory;

public interface IEventEnvelopeFactory
{
    EventEnvelopeV1 CreateBase(string subject, string conversationId, string userId);
}