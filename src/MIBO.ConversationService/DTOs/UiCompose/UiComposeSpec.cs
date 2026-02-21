namespace MIBO.ConversationService.DTOs.UiCompose;

public sealed class UiComposeSpec
{
    // schema output
    public string Schema { get; set; } = "ui.v1";

    // dacă vrei să împachetezi tool results în ui.data automat
    public bool IncludeToolDataSnapshot { get; set; } = true;

    // chei permise în data (whitelist) - decuplare și securitate
    public List<string> AllowedToolRefs { get; set; } = new();

    // bindings/subscriptions: dacă vrei să le normalizezi în ui.v1 (opțional)
    public bool IncludeBindings { get; set; } = true;
    public bool IncludeSubscriptions { get; set; } = true;
}