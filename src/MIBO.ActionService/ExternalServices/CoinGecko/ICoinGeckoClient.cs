using System.Text.Json;

namespace MIBO.ActionService.ExternalServices.CoinGecko;

public interface ICoinGeckoClient
{
    Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken);
}