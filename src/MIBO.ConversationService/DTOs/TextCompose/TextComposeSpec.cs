namespace MIBO.ConversationService.DTOs.TextCompose;

public sealed class TextComposeSpec
{
    // IMPORTANT: default gol, NU "OK"
    public string Template { get; set; } = "";

    public List<TextValueBinding> Bindings { get; set; } = new();

    public string MissingValue { get; set; } = "N/A";

    // NEW (optional, non-breaking): dacă vrei fallback controlat din config
    public string? FallbackTemplate { get; set; } = null; 
}

public sealed class TextValueBinding
{
    public string Key { get; set; } = default!;
    public string ToolRef { get; set; } = default!;
    public string JsonPath { get; set; } = default!;
    public string? Format { get; set; } = null;
}