namespace MIBO.ActionService.ExternalServices.CoinGecko;

public sealed class CoinGeckoOptions
{
    public const string SectionName = "ExternalServices:CoinGecko";
    public const string DefaultBaseUrl = "https://api.coingecko.com/api/v3";

    public string BaseUrl { get; set; } = DefaultBaseUrl;

    public Uri GetBaseUri()
    {
        var normalizedBaseUrl = string.IsNullOrWhiteSpace(BaseUrl)
            ? DefaultBaseUrl
            : BaseUrl.Trim();

        return new Uri($"{normalizedBaseUrl.TrimEnd('/')}/", UriKind.Absolute);
    }
}