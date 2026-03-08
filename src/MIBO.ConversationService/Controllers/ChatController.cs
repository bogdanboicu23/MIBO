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
}
