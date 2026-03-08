using MIBO.ConversationService.DTOs.Actions;
using MIBO.ConversationService.Services.Actions.Handler;
using Microsoft.AspNetCore.Mvc;

namespace MIBO.ConversationService.Controllers;

[ApiController]
[Route("api/v1/action")]
[Route("v1/action")]
public sealed class ActionController : ControllerBase
{
    private readonly IActionHandler _handler;

    public ActionController(IActionHandler handler) => _handler = handler;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ActionEnvelopeV1 action, CancellationToken ct)
    {
        if (!string.Equals(action.Schema, "action.v1", StringComparison.Ordinal))
            return BadRequest(new { error = "Invalid schema" });

        if (string.IsNullOrWhiteSpace(action.ConversationId))
            return BadRequest(new { error = "conversationId is required" });

        if (string.IsNullOrWhiteSpace(action.UserId))
            return BadRequest(new { error = "userId is required" });

        if (string.IsNullOrWhiteSpace(action.Action?.Type))
            return BadRequest(new { error = "action.type is required" });

        var res = await _handler.HandleAsync(action, ct);
        return Ok(res);
    }
}
