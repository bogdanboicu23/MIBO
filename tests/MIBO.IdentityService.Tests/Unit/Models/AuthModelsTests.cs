using FluentAssertions;
using MIBO.IdentityService.Models;

namespace MIBO.IdentityService.Tests.Unit.Models;

public class AuthModelsTests
{
    // ════════════════════════════════════════════
    //  ApiResponse
    // ════════════════════════════════════════════

    [Fact]
    public void ApiResponse_Ok_HasSuccessTrueAndNullMessage()
    {
        var response = ApiResponse.Ok();

        response.Success.Should().BeTrue();
        response.Message.Should().BeNull();
    }

    [Fact]
    public void ApiResponse_Fail_HasSuccessFalseAndMessage()
    {
        var response = ApiResponse.Fail("Something went wrong");

        response.Success.Should().BeFalse();
        response.Message.Should().Be("Something went wrong");
    }

    // ════════════════════════════════════════════
    //  LoginResponse
    // ════════════════════════════════════════════

    [Fact]
    public void LoginResponse_DefaultValues_AreNull()
    {
        var response = new LoginResponse();

        response.AccessToken.Should().BeNull();
        response.RefreshToken.Should().BeNull();
    }

    [Fact]
    public void LoginResponse_CanSetTokens()
    {
        var response = new LoginResponse
        {
            AccessToken = "access",
            RefreshToken = "refresh"
        };

        response.AccessToken.Should().Be("access");
        response.RefreshToken.Should().Be("refresh");
    }

    // ════════════════════════════════════════════
    //  JwtSettingsOptions
    // ════════════════════════════════════════════

    [Fact]
    public void JwtSettingsOptions_DefaultValues()
    {
        var settings = new JwtSettingsOptions();

        settings.AccessTokenSecret.Should().BeEmpty();
        settings.RefreshTokenSecret.Should().BeEmpty();
        settings.Issuer.Should().BeEmpty();
        settings.Audience.Should().BeEmpty();
        settings.AccessTokenExpirationMinutes.Should().Be(15);
        settings.RefreshTokenExpirationDays.Should().Be(7);
    }

    [Fact]
    public void JwtSettingsOptions_SectionName_IsJwtSettings()
    {
        JwtSettingsOptions.SectionName.Should().Be("JwtSettings");
    }
}
