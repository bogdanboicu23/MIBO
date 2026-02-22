namespace MIBO.ConversationService.DTOs.Eventing;

public sealed class NatsJetStreamOptions
{
    public string Url { get; set; } = "nats://nats:4222";

    // Stream config
    public string StreamName { get; set; } = "MIBO_EVENTS";
    public List<string> Subjects { get; set; } = new() { "finance.*", "shop.*" };

    // Consumer config
    public string ConsumerDurableName { get; set; } = "conversationservice_refresh";
    public string ConsumerDeliverGroup { get; set; } = "conversationservice"; // queue-group
    public int AckWaitSeconds { get; set; } = 30;
    public int MaxAckPending { get; set; } = 10_000;
}