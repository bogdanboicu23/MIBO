namespace MIBO.ActionService.ExternalServices.OpenWeatherMap;

public sealed class OpenWeatherMapOptions
{
    public const string SectionName = "ExternalServices:OpenWeatherMap";
    public const string DefaultBaseUrl = "https://api.openweathermap.org";

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
