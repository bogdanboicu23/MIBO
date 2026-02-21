using System.Text.Json;

namespace MIBO.ConversationService.Services.UI;

public interface IUiCatalogProvider
{
    Task<JsonElement> GetCatalogAsync(CancellationToken ct); // JSON object
}