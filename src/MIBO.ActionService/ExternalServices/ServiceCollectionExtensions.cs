using MIBO.ActionService.ExternalServices.Abstractions;
using MIBO.ActionService.ExternalServices.BankService;
using MIBO.ActionService.ExternalServices.DummyJson;
using MIBO.ActionService.ExternalServices.OpenWeatherMap;
using MIBO.ActionService.ExternalServices.Pomodoro;
using MIBO.ActionService.ExternalServices.Spotify;
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

        services.AddOptions<OpenWeatherMapOptions>()
            .Bind(configuration.GetSection(OpenWeatherMapOptions.SectionName));

        services.PostConfigure<OpenWeatherMapOptions>(options =>
        {
            var apiKeyOverride = configuration["OPENWEATHERMAP_API_KEY"];
            if (!string.IsNullOrWhiteSpace(apiKeyOverride))
            {
                options.ApiKey = apiKeyOverride;
            }
        });

        services.AddHttpClient<IOpenWeatherMapClient, OpenWeatherMapClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OpenWeatherMapOptions>>().Value;
            client.BaseAddress = options.GetBaseUri();
        });

        services.AddSingleton<IExternalDataSourceHandler, OpenWeatherMapActionHandler>();

        services.AddSingleton<IExternalDataSourceHandler, PomodoroActionHandler>();

        services.AddHttpClient<ISpotifyApiClient, SpotifyApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.spotify.com/v1/");
        });
        services.AddSingleton<IExternalDataSourceHandler, SpotifyActionHandler>();

        services.AddOptions<BankServiceOptions>()
            .Bind(configuration.GetSection(BankServiceOptions.SectionName));

        services.PostConfigure<BankServiceOptions>(options =>
        {
            var baseUrlOverride = configuration["BANKSERVICE_BASE_URL"];
            if (!string.IsNullOrWhiteSpace(baseUrlOverride))
            {
                options.BaseUrl = baseUrlOverride;
            }
        });

        services.AddHttpClient<IBankServiceClient, BankServiceClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<BankServiceOptions>>().Value;
            client.BaseAddress = options.GetBaseUri();
        });

        services.AddSingleton<IExternalDataSourceHandler, BankServiceActionHandler>();

        return services;
    }
}
