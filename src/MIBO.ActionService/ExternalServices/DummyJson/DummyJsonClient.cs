using System.Net.Http.Json;
using System.Text.Json;

namespace MIBO.ActionService.ExternalServices.DummyJson;

public sealed class DummyJsonClient(HttpClient httpClient) : IDummyJsonClient
{
    public async Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }
}
