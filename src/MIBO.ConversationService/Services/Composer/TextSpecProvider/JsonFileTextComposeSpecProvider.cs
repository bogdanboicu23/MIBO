using System.Text.Json;
using MIBO.ConversationService.DTOs.TextCompose;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Composer.TextSpecProvider;

public sealed class TextComposeSpecOptions
{
    public string Source { get; set; } = default!; // path json
}

public sealed class JsonFileTextComposeSpecProvider : ITextComposeSpecProvider
{
    private readonly TextComposeSpecOptions _opt;

    public JsonFileTextComposeSpecProvider(IOptions<TextComposeSpecOptions> opt) => _opt = opt.Value;

    public async Task<TextComposeSpec> GetSpecAsync(string conversationId, string userId, CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(_opt.Source, ct);
        return JsonSerializer.Deserialize<TextComposeSpec>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
               ?? new TextComposeSpec();
    }
}