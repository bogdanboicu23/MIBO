namespace MIBO.ActionService.ExternalServices.DummyJson;

public sealed class DummyJsonOptions
{
    public const string SectionName = "ExternalServices:DummyJson";
    public const string DefaultBaseUrl = "https://dummyjson.com";

    public string BaseUrl { get; set; } = DefaultBaseUrl;

    public Uri GetBaseUri()
    {
        var normalizedBaseUrl = string.IsNullOrWhiteSpace(BaseUrl)
            ? DefaultBaseUrl
            : BaseUrl.Trim();

        return new Uri($"{normalizedBaseUrl.TrimEnd('/')}/", UriKind.Absolute);
    }
}
