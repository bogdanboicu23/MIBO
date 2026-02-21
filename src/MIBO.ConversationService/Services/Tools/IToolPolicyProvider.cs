using MIBO.ConversationService.DTOs.Tools;
using Polly;

namespace MIBO.ConversationService.Services.Tools;

public interface IToolPolicyProvider
{
    IAsyncPolicy<HttpResponseMessage> GetPolicy(ToolDefinition tool);
}
