using System.Text.Json.Serialization;
using MIBO.ActionService.Services;
using Microsoft.AspNetCore.Mvc;

namespace MIBO.ActionService.Controllers;

[ApiController]
[Route("api/actions")]
public sealed class ActionsController(IActionRouter actionRouter) : ControllerBase
{
    [HttpPost("query")]
    public async Task<ActionResult<QueryResponse>> Query(
        [FromBody] QueryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DataSource is null)
        {
            return BadRequest(new { error = "dataSource is required." });
        }

        var result = await actionRouter.QueryAsync(
            request.DataSource,
            request.Args,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("execute")]
    public async Task<ActionResult<ActionExecutionResponse>> Execute(
        [FromBody] ExecuteActionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Action is null)
        {
            return BadRequest(new { error = "action is required." });
        }

        var result = await actionRouter.ExecuteAsync(
            request.Action,
            request.DataSource,
            request.DataSources,
            request.Payload,
            cancellationToken);

        return Ok(result);
    }
}

public sealed class QueryRequest
{
    [JsonPropertyName("dataSource")]
    public DataSourceDefinition? DataSource { get; init; }

    [JsonPropertyName("args")]
    public Dictionary<string, object?> Args { get; init; } = new();
}

public sealed class ExecuteActionRequest
{
    [JsonPropertyName("action")]
    public ActionDefinition? Action { get; init; }

    [JsonPropertyName("dataSource")]
    public DataSourceDefinition? DataSource { get; init; }

    [JsonPropertyName("dataSources")]
    public Dictionary<string, DataSourceDefinition> DataSources { get; init; } = new();

    [JsonPropertyName("payload")]
    public Dictionary<string, object?> Payload { get; init; } = new();
}
