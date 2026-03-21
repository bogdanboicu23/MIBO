extern alias ConversationService;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MIBO.E2ETests.Fixtures;

public class ConversationServiceFactory : WebApplicationFactory<ConversationService::Program>
{
    private readonly string _mongoConnectionString;
    private readonly string _wireMockAgentUrl;
    private readonly string _wireMockActionUrl;

    public ConversationServiceFactory(
        string mongoConnectionString,
        string wireMockAgentUrl,
        string wireMockActionUrl)
    {
        _mongoConnectionString = mongoConnectionString;
        _wireMockAgentUrl = wireMockAgentUrl;
        _wireMockActionUrl = wireMockActionUrl;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("Mongo:ConnectionString", _mongoConnectionString);
        builder.UseSetting("Mongo:Database", $"mibo_e2e_{Guid.NewGuid():N}");
        builder.UseSetting("AGENT_SERVICE_URL", _wireMockAgentUrl);
        builder.UseSetting("ACTION_SERVICE_URL", _wireMockActionUrl);
    }
}
