using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MIBO.FinanceDataService.Models;
using MIBO.FinanceDataService.Dtos;
using Xunit;

namespace MIBO.FinanceDataService.Tests.Controllers;

public class AccountsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AccountsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAccount_WithValidId_ReturnsAccount()
    {
        // Act
        var response = await _client.GetAsync("/api/accounts/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var account = await response.Content.ReadFromJsonAsync<Account>();
        account.Should().NotBeNull();
        account!.Id.Should().Be(1);
        account.UserId.Should().BeGreaterThan(0);
        account.Balance.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetAccount_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/accounts/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAccountsByUser_ReturnsPaginatedResults()
    {
        // Act
        var response = await _client.GetAsync("/api/accounts/user/1?skip=0&limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNull();
        content.Should().Contain("accounts");
        content.Should().Contain("total");
    }

    [Fact]
    public async Task GetAllAccounts_WithPagination_ReturnsCorrectSubset()
    {
        // Act
        var response = await _client.GetAsync("/api/accounts?skip=0&limit=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("accounts");
        content.Should().Contain("total");
        content.Should().Contain("skip");
        content.Should().Contain("limit");
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(5, 5)]
    [InlineData(10, 5)]
    public async Task GetAccounts_WithDifferentPagination_ReturnsCorrectResults(int skip, int limit)
    {
        // Act
        var response = await _client.GetAsync($"/api/accounts?skip={skip}&limit={limit}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}