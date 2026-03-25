using System.Text.Json;

namespace MIBO.ActionService.ExternalServices.NewsApi;

public interface INewsApiClient
{
    Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken);
}