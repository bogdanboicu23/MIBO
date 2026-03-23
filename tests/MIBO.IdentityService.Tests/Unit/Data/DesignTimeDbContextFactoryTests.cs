using FluentAssertions;
using MIBO.IdentityService.Data;
using Microsoft.EntityFrameworkCore.Design;

namespace MIBO.IdentityService.Tests.Unit.Data;

public class DesignTimeDbContextFactoryTests
{
    [Fact]
    public void Factory_ImplementsIDesignTimeDbContextFactory()
    {
        var factory = new DesignTimeDbContextFactory();

        factory.Should().BeAssignableTo<IDesignTimeDbContextFactory<ApplicationDbContext>>();
    }

    [Fact]
    public void CreateDbContext_ReturnsApplicationDbContext()
    {
        var factory = new DesignTimeDbContextFactory();

        // The factory reads from appsettings.json / env vars.
        // If a connection string is available, it should create a context.
        // If not, it throws InvalidOperationException.
        try
        {
            using var context = factory.CreateDbContext([]);
            context.Should().NotBeNull();
            context.Should().BeOfType<ApplicationDbContext>();
        }
        catch (InvalidOperationException ex)
        {
            ex.Message.Should().Contain("DefaultConnection");
        }
    }
}
