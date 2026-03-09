using MIBO.ConversationService.DTOs.Contracts;
using MIBO.ConversationService.Services.Chat;
using Microsoft.AspNetCore.Mvc;

namespace MIBO.ConversationService.Controllers;

[ApiController]
[Route("api/v1/chat")]
[Route("v1/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatOrchestrator _chat;

    public ChatController(IChatOrchestrator chat) => _chat = chat;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.ConversationId))
            return BadRequest(new { error = "conversationId is required" });
        if (string.IsNullOrWhiteSpace(req.UserId))
            return BadRequest(new { error = "userId is required" });
        if (string.IsNullOrWhiteSpace(req.Prompt))
            return BadRequest(new { error = "prompt is required" });

        var res = await _chat.HandleAsync(req, ct);
        return Ok(res);
    }

    [HttpPost("stream")]
    public async Task Stream([FromBody] ChatRequest req, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        await foreach (var evt in _chat.HandleStreamAsync(req, ct))
        {
            if (evt.EventType is not null)
                await Response.WriteAsync($"event: {evt.EventType}\n", ct);

            await Response.WriteAsync($"data: {evt.Data}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
