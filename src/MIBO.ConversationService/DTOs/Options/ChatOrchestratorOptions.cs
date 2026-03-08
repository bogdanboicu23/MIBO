namespace MIBO.ConversationService.DTOs.Options;

public sealed class ChatOrchestratorOptions
{
    public int MaxToolSteps { get; set; } = 8;

    public int MaxContextChars { get; set; } = 6_000;

    public int MaxToolResultCharsForText { get; set; } = 2_000;

    public bool StrictUiValidation { get; set; } = true;

    public bool PreferInPlaceForActions { get; set; } = true;

    // Dacă planner eșuează (400/500) sau plan invalid, folosim fallback
    public bool EnableFallback { get; set; } = true;

    // Dacă tool failure: poți decide să oprești sau să continui (ex: best-effort)
    public bool BestEffortTools { get; set; } = false;
}
