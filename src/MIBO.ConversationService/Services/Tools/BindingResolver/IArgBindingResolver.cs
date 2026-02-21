using System.Text.Json;

namespace MIBO.ConversationService.Services.Tools.BindingResolver;

public interface IArgBindingResolver
{
    Dictionary<string, object?> Resolve(
        string tool,
        Dictionary<string, object?> args,
        IReadOnlyDictionary<string, JsonElement> toolResults
    );
}