using FluentAssertions;
using MIBO.ActionService.Controllers;
using MIBO.ActionService.RetryPolicy;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace MIBO.ActionService.Tests.Unit.Controllers;

public class StatusControllerTests
{
    private readonly Mock<IExternalServiceStatusQuery> _statusQueryMock = new();
    private readonly StatusController _sut;

    public StatusControllerTests()
    {
        _sut = new StatusController(_statusQueryMock.Object);
    }

    [Fact]
    public async Task GetSummary_ReturnsOkWithSummaryPayload()
    {
        var summary = new ExternalServiceStatusSummary(
            true,
            DateTime.UtcNow,
            "operational",
            new ExternalServiceStatusCounts(3, 0, 3, 0, 0),
            [],
            []);

        _statusQueryMock
            .Setup(query => query.GetSummaryAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var result = await _sut.GetSummary(25, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(summary);
    }
}
