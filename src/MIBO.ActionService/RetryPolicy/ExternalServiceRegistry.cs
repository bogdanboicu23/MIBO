using MIBO.ActionService.ExternalServices.BankService;
using MIBO.ActionService.ExternalServices.CoinGecko;
using MIBO.ActionService.ExternalServices.DummyJson;
using MIBO.ActionService.ExternalServices.NewsApi;
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
    IOptionsMonitor<OpenWeatherMapOptions> openWeatherMapOptions,
    IOptionsMonitor<BankServiceOptions> bankServiceOptions,
    IOptionsMonitor<CoinGeckoOptions> coinGeckoOptions,
    IOptionsMonitor<NewsApiOptions> newsApiOptions)
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
            new ExternalServiceDescriptor(
                "bankservice",
                "BankService",
                "MIBO Finance",
                bankServiceOptions.CurrentValue.GetBaseUri().ToString().TrimEnd('/'),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "finance.accounts.list",
                    "finance.accounts.get",
                    "finance.transactions.list",
                    "finance.transactions.search",
                    "finance.expenses.list",
                    "finance.expenses.categories",
                    "finance.budgets.list",
                    "finance.summary.get",
                    "finance.analytics.expenses",
                    "finance.analytics.transactions",
                }),
            new ExternalServiceDescriptor(
                "coingecko",
                "CoinGecko",
                "CoinGecko",
                coinGeckoOptions.CurrentValue.GetBaseUri().ToString().TrimEnd('/'),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "crypto.markets",
                    "crypto.price",
                    "crypto.trending",
                    "crypto.search",
                    "crypto.coin.detail",
                }),
            new ExternalServiceDescriptor(
                "newsapi",
                "NewsApi",
                "NewsAPI",
                newsApiOptions.CurrentValue.GetBaseUri().ToString().TrimEnd('/'),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "news.headlines",
                    "news.search",
                }),
        ];
    }
}
