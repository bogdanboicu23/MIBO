using MIBO.Storage.Mongo.Store.Conversation;
using Microsoft.AspNetCore.Mvc;

namespace MIBO.ConversationService.Controllers;

[ApiController]
[Route("api/v1/conversations")]
[Route("v1/conversations")]
public sealed class ConversationManagementController : ControllerBase
{
    private readonly IConversationStore _store;

    public ConversationManagementController(IConversationStore store)
    {
        _store = store;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? userId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30,
        CancellationToken ct = default)
    {
        var resolvedUserId = ResolveUserId(userId);
        var items = await _store.ListConversationsAsync(resolvedUserId, skip, limit, ct);
        return Ok(new { items, skip = Math.Max(0, skip), limit = Math.Clamp(limit, 1, 100) });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateConversationRequest? req,
        [FromQuery] string? userId,
        CancellationToken ct)
    {
        var resolvedUserId = ResolveUserId(userId);
        var created = await _store.CreateConversationAsync(resolvedUserId, req?.Title, ct);
        return Ok(created);
    }

    [HttpGet("{conversationId}")]
    public async Task<IActionResult> GetById(
        [FromRoute] string conversationId,
        [FromQuery] string? userId,
        [FromQuery] int messagesLimit = 200,
        CancellationToken ct = default)
    {
        var resolvedUserId = ResolveUserId(userId);
        var result = await _store.GetConversationAsync(conversationId, resolvedUserId, messagesLimit, ct);
        if (result is null) return NotFound(new { error = "Conversation not found" });
        return Ok(result);
    }

    [HttpPatch("{conversationId}")]
    public async Task<IActionResult> Rename(
        [FromRoute] string conversationId,
        [FromBody] RenameConversationRequest req,
        [FromQuery] string? userId,
        CancellationToken ct)
    {
        var resolvedUserId = ResolveUserId(userId);
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { error = "Title is required" });

        var ok = await _store.RenameConversationAsync(conversationId, resolvedUserId, req.Title, ct);
        if (!ok) return NotFound(new { error = "Conversation not found" });

        return Ok(new { success = true });
    }

    [HttpDelete("{conversationId}")]
    public async Task<IActionResult> Delete(
        [FromRoute] string conversationId,
        [FromQuery] string? userId,
        CancellationToken ct)
    {
        var resolvedUserId = ResolveUserId(userId);
        var ok = await _store.DeleteConversationAsync(conversationId, resolvedUserId, ct);
        if (!ok) return NotFound(new { error = "Conversation not found" });
        return Ok(new { success = true });
    }

    private string ResolveUserId(string? userId)
    {
        if (!string.IsNullOrWhiteSpace(userId)) return userId;

        if (Request.Headers.TryGetValue("X-User-Id", out var h) && !string.IsNullOrWhiteSpace(h.FirstOrDefault()))
            return h.First()!;

        if (Request.Headers.TryGetValue("X-Demo-User", out var demo) && !string.IsNullOrWhiteSpace(demo.FirstOrDefault()))
            return demo.First()!;

        return "u-demo-001";
    }

    public sealed class CreateConversationRequest
    {
        public string? Title { get; set; }
    }

    public sealed class RenameConversationRequest
    {
        public string Title { get; set; } = string.Empty;
    }
}
