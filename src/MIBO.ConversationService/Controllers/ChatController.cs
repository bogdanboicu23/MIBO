using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MIBO.Storage.Mongo.Store.Conversation;
using Microsoft.AspNetCore.Mvc;

namespace MIBO.ConversationService.Controllers;

[ApiController]
[Route("api")]
public sealed class ChatController(
    IHttpClientFactory httpClientFactory,
    IConversationStore conversationStore,
    ILogger<ChatController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    [HttpGet("v1/conversations")]
    public async Task<ActionResult<IReadOnlyList<ConversationSummary>>> ListConversations(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = ResolveUserId();
        var conversations = await conversationStore.ListConversationsAsync(userId, skip, limit, cancellationToken);
        return Ok(conversations);
    }

    [HttpPost("v1/conversations")]
    public async Task<ActionResult<ConversationSummary>> CreateConversation(
        [FromBody] CreateConversationRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = ResolveUserId();
        var conversation = await conversationStore.CreateConversationAsync(userId, request?.Title, cancellationToken);
        return Ok(conversation);
    }

    [HttpGet("v1/conversations/{conversationId}")]
    public async Task<ActionResult<ConversationDetails>> GetConversation(
        [FromRoute] string conversationId,
        [FromQuery] int messagesLimit = 200,
        CancellationToken cancellationToken = default)
    {
        var userId = ResolveUserId();
        var conversation = await conversationStore.GetConversationAsync(conversationId, userId, messagesLimit, cancellationToken);

        if (conversation is null)
        {
            return NotFound(new { error = "Conversation not found." });
        }

        return Ok(conversation);
    }

    [HttpPatch("v1/conversations/{conversationId}")]
    public async Task<IActionResult> RenameConversation(
        [FromRoute] string conversationId,
        [FromBody] RenameConversationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { error = "title is required." });
        }

        var userId = ResolveUserId();
        var renamed = await conversationStore.RenameConversationAsync(conversationId, userId, request.Title, cancellationToken);
        return renamed ? Ok(new { ok = true }) : NotFound(new { error = "Conversation not found." });
    }

    [HttpDelete("v1/conversations/{conversationId}")]
    public async Task<IActionResult> DeleteConversation(
        [FromRoute] string conversationId,
        CancellationToken cancellationToken)
    {
        var resolvedUserId = ResolveUserId();
        var deleted = await conversationStore.DeleteConversationAsync(conversationId, resolvedUserId, cancellationToken);
        return deleted ? Ok(new { ok = true }) : NotFound(new { error = "Conversation not found." });
    }

    [HttpPost("chat")]
    public Task PostLegacyChat([FromBody] LegacyChatRequest request, CancellationToken cancellationToken)
        => StreamChatAsync(
            conversationId: request.SessionId,
            userId: ResolveUserId(),
            prompt: request.Message,
            emitSyntheticSessionEvent: string.IsNullOrWhiteSpace(request.SessionId),
            cancellationToken);

    [HttpPost("v1/chat")]
    public Task PostChat([FromBody] ChatRequestV1 request, CancellationToken cancellationToken)
        => StreamChatAsync(
            conversationId: request.ConversationId,
            userId: ResolveUserId(),
            prompt: string.IsNullOrWhiteSpace(request.Prompt) ? request.Message : request.Prompt,
            emitSyntheticSessionEvent: string.IsNullOrWhiteSpace(request.ConversationId),
            cancellationToken);

    private async Task StreamChatAsync(
        string? conversationId,
        string userId,
        string? prompt,
        bool emitSyntheticSessionEvent,
        CancellationToken cancellationToken)
    {
        var trimmedPrompt = prompt?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedPrompt))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(new { error = "Message is required." }, cancellationToken);
            return;
        }

        var effectiveConversationId = conversationId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(effectiveConversationId))
        {
            var createdConversation = await conversationStore.CreateConversationAsync(userId, null, cancellationToken);
            effectiveConversationId = createdConversation.ConversationId;
        }

        var correlationId = Guid.NewGuid().ToString("N");
        try
        {
            await conversationStore.AppendUserMessageAsync(effectiveConversationId, userId, trimmedPrompt, correlationId, cancellationToken);
        }
        catch (ConversationOwnershipException)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await Response.WriteAsJsonAsync(new { error = "Conversation not found." }, cancellationToken);
            return;
        }

        using var upstreamRequest = new HttpRequestMessage(HttpMethod.Post, "/chat")
        {
            Content = JsonContent.Create(new AgentChatRequest(effectiveConversationId, trimmedPrompt, userId))
        };

        var client = httpClientFactory.CreateClient("agent");
        using var upstreamResponse = await client.SendAsync(
            upstreamRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!upstreamResponse.IsSuccessStatusCode)
        {
            Response.StatusCode = (int)upstreamResponse.StatusCode;
            var errorBody = await upstreamResponse.Content.ReadAsStringAsync(cancellationToken);
            await Response.WriteAsync(errorBody, cancellationToken);
            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");
        Response.Headers.Append("X-Conversation-Id", effectiveConversationId);

        await Response.StartAsync(cancellationToken);

        if (emitSyntheticSessionEvent)
        {
            var sessionPayload = JsonSerializer.Serialize(new
            {
                type = "session",
                session_id = effectiveConversationId
            }, JsonOpts);

            await Response.WriteAsync($"data: {sessionPayload}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        await using var upstreamStream = await upstreamResponse.Content.ReadAsStreamAsync(cancellationToken);
        var decoder = Encoding.UTF8;
        var buffer = new byte[8192];
        var rawBuffer = new StringBuilder();

        object? assistantPayload = null;
        string assistantText = string.Empty;

        try
        {
            while (true)
            {
                var bytesRead = await upstreamStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                rawBuffer.Append(decoder.GetString(buffer, 0, bytesRead));

                while (true)
                {
                    var current = rawBuffer.ToString();
                    var boundaryIndex = current.IndexOf("\n\n", StringComparison.Ordinal);
                    if (boundaryIndex < 0)
                    {
                        break;
                    }

                    var rawEvent = current[..boundaryIndex];
                    rawBuffer.Remove(0, boundaryIndex + 2);

                    if (TryReadSseEnvelope(rawEvent, out var eventType, out var content)
                        && string.Equals(eventType, "done", StringComparison.OrdinalIgnoreCase))
                    {
                        if (TryParseAssistantPayload(content, out var parsedPayload, out var parsedText))
                        {
                            assistantPayload = parsedPayload;
                            assistantText = parsedText;
                        }
                        else
                        {
                            assistantPayload = null;
                            assistantText = content;
                        }
                    }

                    await Response.WriteAsync(rawEvent, cancellationToken);
                    await Response.WriteAsync("\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat streaming failed for conversation {ConversationId}", effectiveConversationId);
            throw;
        }

        if (assistantPayload is not null || !string.IsNullOrWhiteSpace(assistantText))
        {
            await conversationStore.AppendAssistantMessageAsync(
                effectiveConversationId,
                userId,
                assistantText,
                assistantPayload,
                correlationId,
                cancellationToken);
        }
    }

    private string ResolveUserId()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var headerUserId))
        {
            var headerValue = headerUserId.ToString();
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                return headerValue.Trim();
            }
        }

        return "anonymous";
    }

    private static bool TryReadSseEnvelope(string rawEvent, out string eventType, out string content)
    {
        eventType = string.Empty;
        content = string.Empty;

        var dataLines = rawEvent
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith("data:", StringComparison.Ordinal))
            .Select(line => line.StartsWith("data: ", StringComparison.Ordinal) ? line[6..] : line[5..])
            .ToArray();

        if (dataLines.Length == 0)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(string.Join("\n", dataLines));
            if (!document.RootElement.TryGetProperty("type", out var typeNode) || typeNode.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            eventType = typeNode.GetString() ?? string.Empty;
            if (document.RootElement.TryGetProperty("content", out var contentNode))
            {
                content = contentNode.ValueKind == JsonValueKind.String
                    ? contentNode.GetString() ?? string.Empty
                    : contentNode.GetRawText();
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryParseAssistantPayload(string rawContent, out object? payload, out string text)
    {
        payload = null;
        text = rawContent;

        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(rawContent);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            payload = JsonSerializer.Deserialize<object>(rawContent, JsonOpts);
            if (document.RootElement.TryGetProperty("text", out var textNode) && textNode.ValueKind == JsonValueKind.String)
            {
                text = textNode.GetString() ?? string.Empty;
            }
            else
            {
                text = string.Empty;
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed class CreateConversationRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }
}

public sealed class RenameConversationRequest
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;
}

public sealed class ChatRequestV1
{
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; init; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

public sealed class LegacyChatRequest
{
    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

public sealed record AgentChatRequest(
    [property: JsonPropertyName("session_id")] string SessionId,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("user_id")] string UserId);
