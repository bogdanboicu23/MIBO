using System.Text.Json;

namespace MIBO.ActionService.ExternalServices.OpenWeatherMap;

public interface IOpenWeatherMapClient
{
    Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken);
}
