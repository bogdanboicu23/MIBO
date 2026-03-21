extern alias ActionService;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MIBO.E2ETests.Fixtures;

public class ActionServiceFactory : WebApplicationFactory<ActionService::Program>
{
    private readonly string _wireMockBaseUrl;

    public ActionServiceFactory(string wireMockBaseUrl)
    {
        _wireMockBaseUrl = wireMockBaseUrl;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("DUMMYJSON_BASE_URL", _wireMockBaseUrl);
    }
}
