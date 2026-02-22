using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.UI;

public sealed class UiCatalogOptions { public string Source { get; set; } = default!; }

public sealed class JsonFileUiCatalogProvider : IUiCatalogProvider
{
    private readonly UiCatalogOptions _opt;
    public JsonFileUiCatalogProvider(IOptions<UiCatalogOptions> opt) => _opt = opt.Value;

    public async Task<JsonElement> GetCatalogAsync(CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(_opt.Source, ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}