using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MIBO.ConversationService.Services;
using MIBO.IdentityService.Models;

namespace MIBO.ConversationService.Controllers;

[ApiController]
[Route("api/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IGroqChatService _chat;

    public ConversationsController(IGroqChatService chat)
    {
        _chat = chat;
    }

    [HttpPost("stream")]
    public async Task Stream([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        // SSE headers
        Response.StatusCode = 200;
        Response.Headers["Content-Type"] = "text/event-stream; charset=utf-8";
        Response.Headers["Cache-Control"] = "no-cache, no-transform";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            await WriteSseEventAsync("error", new { message = "Message is required." }, ct);
            return;
        }

        try
        {
            await foreach (var token in _chat.StreamMessageAsync(request.Message, ct))
            {
                if (ct.IsCancellationRequested || HttpContext.RequestAborted.IsCancellationRequested)
                    break;

                await WriteSseDataAsync(new { t = token }, ct);
            }

            await WriteSseEventAsync("done", new { done = true }, ct);
        }
        catch (OperationCanceledException)
        {
            // Normal: client closed connection or aborted request.
            // Don't write anything; just exit.
        }
        catch (Exception ex)
        {
            // Send structured error (avoid leaking internal details in prod)
            await WriteSseEventAsync("error", new
            {
                message = "Streaming failed."
                // details = ex.Message // (optional) only in dev
            }, ct);
        }
        finally
        {
            try { await Response.Body.FlushAsync(ct); } catch { /* ignore */ }
        }
    }

    // GET health
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "Conversation Service is healthy" });
    }

    private async Task WriteSseDataAsync(object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);

        // Standard SSE: "data: <payload>\n\n"
        await Response.WriteAsync($"data: {json}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }

    private async Task WriteSseEventAsync(string eventName, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);

        await Response.WriteAsync($"event: {eventName}\n", ct);
        await Response.WriteAsync($"data: {json}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
