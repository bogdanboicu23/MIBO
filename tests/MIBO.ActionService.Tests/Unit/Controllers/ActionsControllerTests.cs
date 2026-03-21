using FluentAssertions;
using MIBO.ActionService.Controllers;
using MIBO.ActionService.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace MIBO.ActionService.Tests.Unit.Controllers;

public class ActionsControllerTests
{
    private readonly Mock<IActionRouter> _routerMock = new();
    private readonly ActionsController _sut;

    public ActionsControllerTests()
    {
        _sut = new ActionsController(_routerMock.Object);
    }

    // ════════════════════════════════════════════
    //  QUERY
    // ════════════════════════════════════════════

    #region Query

    [Fact]
    public async Task Query_NullDataSource_ReturnsBadRequest()
    {
        // Arrange
        var request = new QueryRequest { DataSource = null };

        // Act
        var result = await _sut.Query(request, CancellationToken.None);

        // Assert
        var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().BeEquivalentTo(new { error = "dataSource is required." });
    }

    [Fact]
    public async Task Query_ValidRequest_ReturnsOk()
    {
        // Arrange
        var dataSource = new DataSourceDefinition { Id = "ds1", Handler = "products.catalog.query" };
        var expectedResponse = new QueryResponse { DataSourceId = "ds1", Handler = "products.catalog.query" };

        _routerMock
            .Setup(x => x.QueryAsync(dataSource, It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var request = new QueryRequest { DataSource = dataSource };

        // Act
        var result = await _sut.Query(request, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Query_PassesArgsThroughToRouter()
    {
        // Arrange
        var dataSource = new DataSourceDefinition { Id = "ds1", Handler = "finance.user.summary" };
        var args = new Dictionary<string, object?> { ["userId"] = 42 };

        _routerMock
            .Setup(x => x.QueryAsync(dataSource, It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResponse { DataSourceId = "ds1" });

        var request = new QueryRequest { DataSource = dataSource, Args = args };

        // Act
        await _sut.Query(request, CancellationToken.None);

        // Assert — verify args were passed through
        _routerMock.Verify(
            x => x.QueryAsync(dataSource, args, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ════════════════════════════════════════════
    //  EXECUTE
    // ════════════════════════════════════════════

    #region Execute

    [Fact]
    public async Task Execute_NullAction_ReturnsBadRequest()
    {
        // Arrange
        var request = new ExecuteActionRequest { Action = null };

        // Act
        var result = await _sut.Execute(request, CancellationToken.None);

        // Assert
        var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().BeEquivalentTo(new { error = "action is required." });
    }

    [Fact]
    public async Task Execute_ValidRequest_ReturnsOk()
    {
        // Arrange
        var action = new ActionDefinition { Id = "act1", ActionType = "ui.action.execute" };
        var expectedResponse = new ActionExecutionResponse { ActionId = "act1", ActionType = "ui.action.execute" };

        _routerMock
            .Setup(x => x.ExecuteAsync(
                action,
                It.IsAny<DataSourceDefinition?>(),
                It.IsAny<IReadOnlyDictionary<string, DataSourceDefinition>?>(),
                It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var request = new ExecuteActionRequest { Action = action };

        // Act
        var result = await _sut.Execute(request, CancellationToken.None);

        // Assert
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Execute_PassesAllFieldsThroughToRouter()
    {
        // Arrange
        var action = new ActionDefinition { Id = "act1" };
        var dataSource = new DataSourceDefinition { Id = "ds1" };
        var dataSources = new Dictionary<string, DataSourceDefinition> { ["ds2"] = new() { Id = "ds2" } };
        var payload = new Dictionary<string, object?> { ["key"] = "value" };

        _routerMock
            .Setup(x => x.ExecuteAsync(
                action, dataSource, dataSources, payload, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActionExecutionResponse { ActionId = "act1" });

        var request = new ExecuteActionRequest
        {
            Action = action,
            DataSource = dataSource,
            DataSources = dataSources,
            Payload = payload
        };

        // Act
        await _sut.Execute(request, CancellationToken.None);

        // Assert
        _routerMock.Verify(
            x => x.ExecuteAsync(action, dataSource, dataSources, payload, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}