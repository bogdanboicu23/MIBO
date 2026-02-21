namespace MIBO.ConversationService.Middleware.Http;


public sealed record HeaderContext(string CorrelationId, string UserId, string ConversationId);

public interface IHeaderContextAccessor
{
    HeaderContext Get();
}
