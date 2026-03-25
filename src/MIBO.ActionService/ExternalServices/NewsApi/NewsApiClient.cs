using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MIBO.ActionService.ExternalServices.NewsApi;

public sealed class NewsApiClient(HttpClient httpClient, IOptions<NewsApiOptions> options)
    : INewsApiClient
{
    public async Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken)
    {
        var separator = path.Contains('?') ? '&' : '?';
        var url = $"{path}{separator}apiKey={options.Value.ApiKey}";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }
}