namespace MIBO.ConversationService.DTOs.Tools;

public sealed record ToolDefinition(
    string Name,              // e.g. "finance.getBudgetSnapshot"
    string Method,            // GET/POST
    string UrlTemplate,       // e.g. "http://finance-data/api/v1/budget-snapshot"
    int TimeoutMs,
    int RetryCount,
    int CacheTtlSeconds,
    string[] RequiredArgs     // e.g. ["rangeDays"] or empty
);
