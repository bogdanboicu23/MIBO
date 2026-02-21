using System.Text.Json;
using MIBO.ConversationService.DTOs.Actions;
using MIBO.ConversationService.Helper;

namespace MIBO.ConversationService.Services.Eventing.Builder;

public sealed class DefaultEventPayloadBuilder : IEventPayloadBuilder
{
    public Dictionary<string, object?> Build(PublishSpec spec, Dictionary<string, object?> actionPayload, JsonElement toolBody)
    {
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // fixed
        foreach (var (k, v) in spec.Fixed) payload[k] = v;

        // from action payload
        foreach (var (dst, srcKey) in spec.FromActionPayload)
        {
            actionPayload.TryGetValue(srcKey, out var v);
            payload[dst] = v;
        }

        // from tool result json
        foreach (var (dst, path) in spec.FromToolResultJsonPath)
        {
            if (JsonPathLite.TryGet(toolBody, path, out var el))
                payload[dst] = JsonElementToDotNet(el);
            else
                payload[dst] = null;
        }

        return payload;
    }

    private static object? JsonElementToDotNet(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetDecimal(out var d) ? d : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Object => JsonSerializer.Deserialize<object>(el.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            JsonValueKind.Array => JsonSerializer.Deserialize<object>(el.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            _ => null
        };
    }
}