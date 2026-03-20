using MIBO.ActionService.ExternalServices.Abstractions;
using MIBO.ActionService.ExternalServices.DummyJson;
using Microsoft.Extensions.Options;

namespace MIBO.ActionService.ExternalServices;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExternalServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DummyJsonOptions>()
            .Bind(configuration.GetSection(DummyJsonOptions.SectionName));

        services.PostConfigure<DummyJsonOptions>(options =>
        {
            var baseUrlOverride = configuration["DUMMYJSON_BASE_URL"];
            if (!string.IsNullOrWhiteSpace(baseUrlOverride))
            {
                options.BaseUrl = baseUrlOverride;
            }
        });

        services.AddHttpClient<IDummyJsonClient, DummyJsonClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<DummyJsonOptions>>().Value;
            client.BaseAddress = options.GetBaseUri();
        });

        services.AddSingleton<IExternalDataSourceHandler, DummyJsonActionHandler>();

        return services;
    }
}
