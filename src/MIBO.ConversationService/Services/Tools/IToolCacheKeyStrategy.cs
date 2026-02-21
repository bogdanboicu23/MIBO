namespace MIBO.ConversationService.Services.Tools;

public interface IToolCacheKeyStrategy
{
    string Build(string toolName, string url, string userId);
}