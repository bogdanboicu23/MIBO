using MIBO.ConversationService.DTOs.Tools;

namespace MIBO.ConversationService.Services.Tools;


public interface IToolRegistry
{
    ToolDefinition Get(string name);
    bool TryGet(string name, out ToolDefinition? def);
    IReadOnlyCollection<ToolDefinition> All();
    Task RefreshAsync(CancellationToken ct);
}