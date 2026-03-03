using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace MIBO.TestInfrastructure.TestBase;

public abstract class ServiceTestBase<TStartup> : IClassFixture<WebApplicationFactory<TStartup>>, IAsyncLifetime
    where TStartup : class
{
    protected readonly WebApplicationFactory<TStartup> Factory;
    protected HttpClient Client { get; private set; }
    protected IServiceScope Scope { get; private set; }

    protected ServiceTestBase(WebApplicationFactory<TStartup> factory)
    {
        Factory = factory;
    }

    public virtual async Task InitializeAsync()
    {
        Client = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                ConfigureTestServices(services);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        Scope = Factory.Services.CreateScope();

        await SeedTestDataAsync();
    }

    public virtual Task DisposeAsync()
    {
        Scope?.Dispose();
        Client?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override this method to configure test-specific services
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Override in derived classes to replace services with test doubles
    }

    /// <summary>
    /// Override this method to seed test data
    /// </summary>
    protected virtual Task SeedTestDataAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Helper method to add authorization header
    /// </summary>
    protected void AddAuthorizationHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Helper method to get service from DI container
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return Scope.ServiceProvider.GetRequiredService<T>();
    }
}