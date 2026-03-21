using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MIBO.ActionService.ExternalServices.OpenWeatherMap;

public sealed class OpenWeatherMapClient(HttpClient httpClient, IOptions<OpenWeatherMapOptions> options)
    : IOpenWeatherMapClient
{
    public async Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken)
    {
        var separator = path.Contains('?') ? '&' : '?';
        var url = $"{path}{separator}appid={options.Value.ApiKey}&units=metric";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }
}
