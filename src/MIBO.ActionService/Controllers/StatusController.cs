using MIBO.ActionService.RetryPolicy;
using Microsoft.AspNetCore.Mvc;

namespace MIBO.ActionService.Controllers;

[ApiController]
[Route("api/actions/status")]
public sealed class StatusController(IExternalServiceStatusQuery statusQuery) : ControllerBase
{
    // Status Controller
    [HttpGet("summary")]
    public async Task<ActionResult<ExternalServiceStatusSummary>> GetSummary(
        [FromQuery] int auditLimit = 50,
        CancellationToken cancellationToken = default)
    {
        var summary = await statusQuery.GetSummaryAsync(auditLimit, cancellationToken);
        return Ok(summary);
    }
}
