using MIBO.ConversationService.DTOs.Tools;

namespace MIBO.ConversationService.Services.Tools;

public interface IToolCatalogProvider
{
    Task<IReadOnlyCollection<ToolDefinition>> GetToolsAsync(CancellationToken ct);
}