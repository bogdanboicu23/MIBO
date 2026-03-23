using System.Text.Json;

namespace MIBO.ActionService.ExternalServices.BankService;

public interface IBankServiceClient
{
    Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken);
}