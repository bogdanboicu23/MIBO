namespace MIBO.ConversationService.DTOs.Options;

public sealed class ToolCatalogOptions
{
    // ex: "tools.json" sau un URL de config-service; provider-ul decide cum îl folosește
    public string Source { get; set; } = default!;

    // Reload interval pentru provider (dacă suportă)
    public int ReloadSeconds { get; set; } = 60;
}