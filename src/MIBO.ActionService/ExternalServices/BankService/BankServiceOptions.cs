namespace MIBO.ActionService.ExternalServices.BankService;

public sealed class BankServiceOptions
{
    public const string SectionName = "ExternalServices:BankService";
    public const string DefaultBaseUrl = "http://localhost:5148";

    public string BaseUrl { get; set; } = DefaultBaseUrl;

    public Uri GetBaseUri()
    {
        var normalizedBaseUrl = string.IsNullOrWhiteSpace(BaseUrl)
            ? DefaultBaseUrl
            : BaseUrl.Trim();

        return new Uri($"{normalizedBaseUrl.TrimEnd('/')}/", UriKind.Absolute);
    }
}