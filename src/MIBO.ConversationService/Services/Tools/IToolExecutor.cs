using MIBO.ConversationService.DTOs.Tools;

namespace MIBO.ConversationService.Services.Tools;

public interface IToolExecutor
{
    Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct);
}