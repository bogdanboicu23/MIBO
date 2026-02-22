namespace MIBO.ConversationService.DTOs.Options;

public sealed class ChatOrchestratorOptions
{
    public int MaxToolSteps { get; set; } = 8;

    // Dacă planner eșuează (400/500) sau plan invalid, folosim fallback
    public bool EnableFallback { get; set; } = true;

    // Dacă tool failure: poți decide să oprești sau să continui (ex: best-effort)
    public bool BestEffortTools { get; set; } = false;
}