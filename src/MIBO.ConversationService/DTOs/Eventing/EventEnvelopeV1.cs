namespace MIBO.ConversationService.DTOs.Eventing;

public sealed class EventEnvelopeV1
{
    public string Schema { get; set; } = "event.v1";
    public string Subject { get; set; } = default!;          // ex: "finance.expense_created"
    public string EventId { get; set; } = default!;          // guid
    public string CorrelationId { get; set; } = default!;
    public string ConversationId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public Dictionary<string, object?> Payload { get; set; } = new();
}