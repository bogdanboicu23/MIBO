using MIBO.ConversationService.DTOs.Contracts;
using MIBO.ConversationService.Services.Chat;
using Microsoft.AspNetCore.Mvc;

namespace MIBO.ConversationService.Controllers;

[ApiController]
[Route("api/v1/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatOrchestrator _chat;

    public ChatController(IChatOrchestrator chat) => _chat = chat;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest req, CancellationToken ct)
    {
        var res = await _chat.HandleAsync(req, ct);
        return Ok(res);
    }
}