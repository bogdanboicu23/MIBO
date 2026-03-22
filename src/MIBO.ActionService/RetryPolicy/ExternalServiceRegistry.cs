using MIBO.ActionService.ExternalServices.DummyJson;
using MIBO.ActionService.ExternalServices.OpenWeatherMap;
using Microsoft.Extensions.Options;

namespace MIBO.ActionService.RetryPolicy;

public sealed record ExternalServiceDescriptor(
    string ServiceKey,
    string ServiceName,
    string DisplayName,
    string BaseUrl,
    IReadOnlySet<string> SupportedHandlers
);

public interface IExternalServiceRegistry
{
    ExternalServiceDescriptor? Resolve(string handler);
    IReadOnlyList<ExternalServiceDescriptor> GetKnownServices();
}

public sealed class ExternalServiceRegistry(
    IOptionsMonitor<DummyJsonOptions> dummyJsonOptions,
    IOptionsMonitor<OpenWeatherMapOptions> openWeatherMapOptions)
    : IExternalServiceRegistry
{
    public ExternalServiceDescriptor? Resolve(string handler)
    {
        if (string.IsNullOrWhiteSpace(handler))
        {
            return null;
        }

        return GetKnownServices().FirstOrDefault(service => service.SupportedHandlers.Contains(handler));
    }

    public IReadOnlyList<ExternalServiceDescriptor> GetKnownServices()
    {
        return
        [
            new ExternalServiceDescriptor(
                "dummyjson",
                "DummyJson",
                "DummyJSON",
                dummyJsonOptions.CurrentValue.GetBaseUri().ToString().TrimEnd('/'),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "products.catalog.query",
                    "products.categories.list",
                    "products.detail.get",
                }),
            new ExternalServiceDescriptor(
                "openweathermap",
                "OpenWeatherMap",
                "OpenWeatherMap",
                openWeatherMapOptions.CurrentValue.GetBaseUri().ToString().TrimEnd('/'),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "weather.current.get",
                    "weather.forecast.get",
                }),
            new ExternalServiceDescriptor(
                "spotify",
                "Spotify",
                "Spotify",
                "https://api.spotify.com/v1",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "spotify.search",
                    "spotify.now_playing",
                    "spotify.playlists",
                    "spotify.top_tracks",
                    "spotify.top_artists",
                }),
        ];
    }
}
