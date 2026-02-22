namespace MIBO.ConversationService.DTOs.Compose;
public sealed class ToolExecutorOptions
{
    public int DefaultTimeoutMs { get; set; } = 2000;
    public int DefaultRetryCount { get; set; } = 2;
    public int DefaultCacheTtlSeconds { get; set; } = 30;
    public int MaxResponseBytes { get; set; } = 2_000_000;

    // NEW:
    public List<string> AllowedHosts { get; set; } = new(); // ex: ["dummyjson.com", "finance-data-service", ...]
}
