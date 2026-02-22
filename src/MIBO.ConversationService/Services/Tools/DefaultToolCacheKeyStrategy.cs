using System.Security.Cryptography;
using System.Text;

namespace MIBO.ConversationService.Services.Tools;

public sealed class DefaultToolCacheKeyStrategy : IToolCacheKeyStrategy
{
    public string Build(string toolName, string url, string userId)
    {
        // hash url to avoid huge keys
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        var h = Convert.ToHexString(bytes).ToLowerInvariant();
        return $"toolcache:v1:{toolName}:u:{userId}:h:{h}";
    }
}