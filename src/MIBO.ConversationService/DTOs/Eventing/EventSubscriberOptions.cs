namespace MIBO.ConversationService.DTOs.Eventing;

public sealed class EventSubscriberOptions
{
    public List<string> Subjects { get; set; } = new() { "finance.>", "shop.>" };
}