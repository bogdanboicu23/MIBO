using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using MIBO.IdentityService.Models;

namespace MIBO.IdentityService.Tests.Unit.Models;

public class JwtSettingsOptionsTests
{
    private static IList<ValidationResult> ValidateModel(JwtSettingsOptions model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    private static JwtSettingsOptions CreateValid() => new()
    {
        AccessTokenSecret = "super-secret-key-that-is-long-enough",
        RefreshTokenSecret = "another-super-secret-key-for-refresh",
        Issuer = "https://localhost",
        Audience = "https://localhost",
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    };

    // ════════════════════════════════════════════
    //  Valid options
    // ════════════════════════════════════════════

    [Fact]
    public void Validate_ValidOptions_PassesValidation()
    {
        var options = CreateValid();

        var results = ValidateModel(options);

        results.Should().BeEmpty();
    }

    // ════════════════════════════════════════════
    //  Required string fields
    // ════════════════════════════════════════════

    [Fact]
    public void Validate_EmptyAccessTokenSecret_FailsRequired()
    {
        var options = CreateValid();
        options.AccessTokenSecret = string.Empty;

        var results = ValidateModel(options);

        results.Should().ContainSingle(r => r.MemberNames.Contains("AccessTokenSecret"));
    }

    [Fact]
    public void Validate_EmptyRefreshTokenSecret_FailsRequired()
    {
        var options = CreateValid();
        options.RefreshTokenSecret = string.Empty;

        var results = ValidateModel(options);

        results.Should().ContainSingle(r => r.MemberNames.Contains("RefreshTokenSecret"));
    }

    [Fact]
    public void Validate_EmptyIssuer_FailsRequired()
    {
        var options = CreateValid();
        options.Issuer = string.Empty;

        var results = ValidateModel(options);

        results.Should().ContainSingle(r => r.MemberNames.Contains("Issuer"));
    }

    [Fact]
    public void Validate_EmptyAudience_FailsRequired()
    {
        var options = CreateValid();
        options.Audience = string.Empty;

        var results = ValidateModel(options);

        results.Should().ContainSingle(r => r.MemberNames.Contains("Audience"));
    }

    // ════════════════════════════════════════════
    //  Range: AccessTokenExpirationMinutes [1, 1440]
    // ════════════════════════════════════════════

    [Fact]
    public void Validate_AccessTokenMinutesZero_FailsRange()
    {
        var options = CreateValid();
        options.AccessTokenExpirationMinutes = 0;

        var results = ValidateModel(options);

        results.Should().ContainSingle(r => r.MemberNames.Contains("AccessTokenExpirationMinutes"));
    }

    [Fact]
    public void Validate_AccessTokenMinutesAboveMax_FailsRange()
    {
        var options = CreateValid();
        options.AccessTokenExpirationMinutes = 1441;

        var results = ValidateModel(options);

        results.Should().ContainSingle(r => r.MemberNames.Contains("AccessTokenExpirationMinutes"));
    }

    [Fact]
    public void Validate_AccessTokenMinutesBoundaryMin_Passes()
    {
        var options = CreateValid();
        options.AccessTokenExpirationMinutes = 1;

        var results = ValidateModel(options);

        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_AccessTokenMinutesBoundaryMax_Passes()
    {
        var options = CreateValid();
        options.AccessTokenExpirationMinutes = 1440;

        var results = ValidateModel(options);

        results.Should().BeEmpty();
    }

    // ════════════════════════════════════════════
    //  Range: RefreshTokenExpirationDays [1, 365]
    // ════════════════════════════════════════════

    [Fact]
    public void Validate_RefreshTokenDaysZero_FailsRange()
    {
        var options = CreateValid();
        options.RefreshTokenExpirationDays = 0;

        var results = ValidateModel(options);

        results.Should().ContainSingle(r => r.MemberNames.Contains("RefreshTokenExpirationDays"));
    }

    [Fact]
    public void Validate_RefreshTokenDaysAboveMax_FailsRange()
    {
        var options = CreateValid();
        options.RefreshTokenExpirationDays = 366;

        var results = ValidateModel(options);

        results.Should().ContainSingle(r => r.MemberNames.Contains("RefreshTokenExpirationDays"));
    }

    [Fact]
    public void Validate_RefreshTokenDaysBoundaryMin_Passes()
    {
        var options = CreateValid();
        options.RefreshTokenExpirationDays = 1;

        var results = ValidateModel(options);

        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_RefreshTokenDaysBoundaryMax_Passes()
    {
        var options = CreateValid();
        options.RefreshTokenExpirationDays = 365;

        var results = ValidateModel(options);

        results.Should().BeEmpty();
    }
}
