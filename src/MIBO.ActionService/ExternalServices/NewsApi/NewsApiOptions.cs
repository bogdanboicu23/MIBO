namespace MIBO.ActionService.ExternalServices.NewsApi;

public sealed class NewsApiOptions
{
    public const string SectionName = "ExternalServices:NewsApi";
    public const string DefaultBaseUrl = "https://newsapi.org/v2";

    public string BaseUrl { get; set; } = DefaultBaseUrl;
    public string ApiKey { get; set; } = string.Empty;

    public Uri GetBaseUri()
    {
        var normalizedBaseUrl = string.IsNullOrWhiteSpace(BaseUrl)
            ? DefaultBaseUrl
            : BaseUrl.Trim();

        return new Uri($"{normalizedBaseUrl.TrimEnd('/')}/", UriKind.Absolute);
    }
}