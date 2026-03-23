using FluentAssertions;
using MIBO.IdentityService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MIBO.IdentityService.Tests.Unit.Data;

public class ApplicationDbContextTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;

    public ApplicationDbContextTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Identity requires logging to build the model
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }

    // ════════════════════════════════════════════
    //  Model configuration
    // ════════════════════════════════════════════

    [Fact]
    public void FirstName_HasMaxLength100()
    {
        var property = _context.Model
            .FindEntityType(typeof(ApplicationUser))!
            .FindProperty(nameof(ApplicationUser.FirstName))!;

        property.GetMaxLength().Should().Be(100);
    }

    [Fact]
    public void LastName_HasMaxLength100()
    {
        var property = _context.Model
            .FindEntityType(typeof(ApplicationUser))!
            .FindProperty(nameof(ApplicationUser.LastName))!;

        property.GetMaxLength().Should().Be(100);
    }

    [Fact]
    public void Context_InheritsFromIdentityDbContext()
    {
        _context.Should().BeAssignableTo<Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<ApplicationUser>>();
    }
}
