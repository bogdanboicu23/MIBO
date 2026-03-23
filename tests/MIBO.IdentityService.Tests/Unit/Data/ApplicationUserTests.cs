using FluentAssertions;
using MIBO.IdentityService.Data;

namespace MIBO.IdentityService.Tests.Unit.Data;

public class ApplicationUserTests
{
    // ════════════════════════════════════════════
    //  Default values
    // ════════════════════════════════════════════

    [Fact]
    public void NewUser_CreatedAt_IsCloseToUtcNow()
    {
        var before = DateTime.UtcNow;

        var user = new ApplicationUser();

        var after = DateTime.UtcNow;
        user.CreatedAt.Should().BeOnOrAfter(before);
        user.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void NewUser_LastLoginAt_IsNull()
    {
        var user = new ApplicationUser();

        user.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public void NewUser_FirstName_IsNull()
    {
        var user = new ApplicationUser();

        user.FirstName.Should().BeNull();
    }

    [Fact]
    public void NewUser_LastName_IsNull()
    {
        var user = new ApplicationUser();

        user.LastName.Should().BeNull();
    }

    [Fact]
    public void NewUser_RefreshToken_IsNull()
    {
        var user = new ApplicationUser();

        user.RefreshToken.Should().BeNull();
    }

    // ════════════════════════════════════════════
    //  Round-trip set/get
    // ════════════════════════════════════════════

    [Fact]
    public void FirstName_SetAndGet_RoundTrips()
    {
        var user = new ApplicationUser { FirstName = "John" };

        user.FirstName.Should().Be("John");
    }

    [Fact]
    public void LastName_SetAndGet_RoundTrips()
    {
        var user = new ApplicationUser { LastName = "Doe" };

        user.LastName.Should().Be("Doe");
    }

    [Fact]
    public void RefreshToken_SetAndGet_RoundTrips()
    {
        var user = new ApplicationUser { RefreshToken = "token-123" };

        user.RefreshToken.Should().Be("token-123");
    }

    [Fact]
    public void LastLoginAt_SetAndGet_RoundTrips()
    {
        var now = DateTime.UtcNow;
        var user = new ApplicationUser { LastLoginAt = now };

        user.LastLoginAt.Should().Be(now);
    }

    [Fact]
    public void CreatedAt_SetAndGet_RoundTrips()
    {
        var custom = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var user = new ApplicationUser { CreatedAt = custom };

        user.CreatedAt.Should().Be(custom);
    }
}
