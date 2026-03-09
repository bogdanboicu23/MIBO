namespace MIBO.ConversationService.DTOs.Contracts;

/// <summary>
/// Represents a single Server-Sent Event to write to the response stream.
/// </summary>
/// <param name="EventType">Optional SSE event name (e.g. "ui", "done", "error"). Null = default/unnamed event.</param>
/// <param name="Data">JSON string to write in the data: field.</param>
public sealed record SseEvent(string? EventType, string Data);