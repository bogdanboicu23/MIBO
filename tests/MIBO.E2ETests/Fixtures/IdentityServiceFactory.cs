extern alias IdentityService;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using IdentityService::MIBO.IdentityService.Data;

namespace MIBO.E2ETests.Fixtures;

public class IdentityServiceFactory : WebApplicationFactory<IdentityService::Program>
{
    private readonly string _postgresConnectionString;

    public IdentityServiceFactory(string postgresConnectionString)
    {
        _postgresConnectionString = postgresConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // UseSetting applies config BEFORE the builder reads it at build-time,
        // unlike ConfigureAppConfiguration which adds sources too late for
        // inline reads like builder.Configuration.GetSection(...).
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgresConnectionString);
        builder.UseSetting("JwtSettings:AccessTokenSecret", "E2ETestAccessTokenSecretKeyThatIsLongEnough1234567890");
        builder.UseSetting("JwtSettings:RefreshTokenSecret", "E2ETestRefreshTokenSecretKeyThatIsLongEnough1234567890");
        builder.UseSetting("JwtSettings:Issuer", "e2e-test-issuer");
        builder.UseSetting("JwtSettings:Audience", "e2e-test-audience");
        builder.UseSetting("JwtSettings:AccessTokenExpirationMinutes", "60");
        builder.UseSetting("JwtSettings:RefreshTokenExpirationDays", "7");
        builder.UseSetting("Turnstile:Enabled", "false");

        builder.ConfigureServices(services =>
        {
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
