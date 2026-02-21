using System.Text.Json;
using MIBO.ConversationService.DTOs.Actions;

namespace MIBO.ConversationService.Services.Eventing.Builder;

public interface IEventPayloadBuilder
{
    Dictionary<string, object?> Build(PublishSpec spec, Dictionary<string, object?> actionPayload, JsonElement toolBody);
}