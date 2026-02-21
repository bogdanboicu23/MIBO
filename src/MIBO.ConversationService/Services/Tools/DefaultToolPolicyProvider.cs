using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.DTOs.Tools;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace MIBO.ConversationService.Services.Tools;

public sealed class DefaultToolPolicyProvider : IToolPolicyProvider
{
    private readonly ToolExecutorOptions _opt;

    public DefaultToolPolicyProvider(IOptions<ToolExecutorOptions> opt) => _opt = opt.Value;

    public IAsyncPolicy<HttpResponseMessage> GetPolicy(ToolDefinition tool)
    {
        var retryCount = tool.RetryCount > 0 ? tool.RetryCount : _opt.DefaultRetryCount;
        var timeoutMs = tool.TimeoutMs > 0 ? tool.TimeoutMs : _opt.DefaultTimeoutMs;

        var retry = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(retryCount, i => TimeSpan.FromMilliseconds(150 * i));

        var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(timeoutMs));

        return Policy.WrapAsync(retry, timeout);
    }
}