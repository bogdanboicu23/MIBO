namespace MIBO.ConversationService.Middleware.Http;


public sealed class HeaderContextMiddleware : IMiddleware, IHeaderContextAccessor
{
    private static readonly AsyncLocal<HeaderContext?> _ctx = new();

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N");
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "anonymous";
        var convId = context.Request.Headers["X-Conversation-Id"].FirstOrDefault() ?? "unknown";

        _ctx.Value = new HeaderContext(correlationId, userId, convId);

        context.Response.Headers["X-Correlation-Id"] = correlationId;
        await next(context);
    }

    public HeaderContext Get()
        => _ctx.Value ?? new HeaderContext(Guid.NewGuid().ToString("N"), "anonymous", "unknown");
}