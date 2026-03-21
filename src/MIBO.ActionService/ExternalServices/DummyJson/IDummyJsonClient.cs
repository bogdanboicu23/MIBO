using System.Text.Json;

namespace MIBO.ActionService.ExternalServices.DummyJson;

public interface IDummyJsonClient
{
    Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken);
}
