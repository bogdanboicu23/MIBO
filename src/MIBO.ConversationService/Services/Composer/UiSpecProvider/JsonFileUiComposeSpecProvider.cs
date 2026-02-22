using System.Text.Json;
using MIBO.ConversationService.DTOs.UiCompose;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Composer.UiSpecProvider;

public sealed class UiComposeSpecOptions { public string Source { get; set; } = default!; }

public sealed class JsonFileUiComposeSpecProvider : IUiComposeSpecProvider
{
    private readonly UiComposeSpecOptions _opt;
    public JsonFileUiComposeSpecProvider(IOptions<UiComposeSpecOptions> opt) => _opt = opt.Value;

    public async Task<UiComposeSpec> GetSpecAsync(string conversationId, string userId, CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(_opt.Source, ct);
        return JsonSerializer.Deserialize<UiComposeSpec>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
               ?? new UiComposeSpec();
    }
}