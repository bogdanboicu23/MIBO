using System.Security.Claims;
using MIBO.Cache.Redis.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MIBO.IdentityService.Controllers;

[ApiController]
[Route("api/auth/finance")]
[Authorize]
public sealed class FinanceController(IFinanceConnectionStore financeStore) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var connected = await financeStore.IsConnectedAsync(userId, ct);
        return Ok(new { connected });
    }

    [HttpPost("connect")]
    public async Task<IActionResult> Connect(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        await financeStore.SetConnectedAsync(userId, ct);
        return Ok(new { connected = true });
    }

    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        await financeStore.SetDisconnectedAsync(userId, ct);
        return Ok(new { disconnected = true });
    }
}
