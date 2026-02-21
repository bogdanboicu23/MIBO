using MIBO.ConversationService.DTOs.Actions;
using MIBO.ConversationService.DTOs.Tools;
using MIBO.ConversationService.Services.Actions.Router;
using MIBO.ConversationService.Services.Eventing;
using MIBO.ConversationService.Services.Eventing.Builder;
using MIBO.ConversationService.Services.Eventing.Factory;
using MIBO.ConversationService.Services.Eventing.JetStream;
using MIBO.ConversationService.Services.Tools;

namespace MIBO.ConversationService.Services.Actions.Handler;

public sealed class ActionHandler : IActionHandler
{
    private readonly IActionRouter _router;
    private readonly IToolExecutor _tools;
    private readonly IEventPublisher _events;
    private readonly IEventEnvelopeFactory _envelopeFactory;
    private readonly IEventPayloadBuilder _payloadBuilder;

    public ActionHandler(
        IActionRouter router,
        IToolExecutor tools,
        IEventPublisher events,
        IEventEnvelopeFactory envelopeFactory,
        IEventPayloadBuilder payloadBuilder)
    {
        _router = router;
        _tools = tools;
        _events = events;
        _envelopeFactory = envelopeFactory;
        _payloadBuilder = payloadBuilder;
    }

    public async Task<ActionResultV1> HandleAsync(ActionEnvelopeV1 action, CancellationToken ct)
    {
        var route = await _router.ResolveAsync(action.Action.Type, ct);

        var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (payloadKey, toolArgKey) in route.ArgMap)
        {
            action.Action.Payload.TryGetValue(payloadKey, out var val);
            args[toolArgKey] = val;
        }

        var toolRes = await _tools.ExecuteAsync(new ToolCall(route.Tool, args), ct);

        if (route.Publish is not null && toolRes.StatusCode is >= 200 and < 300)
        {
            var evt = _envelopeFactory.CreateBase(route.Publish.Subject, action.ConversationId, action.UserId);
            evt.Payload = _payloadBuilder.Build(route.Publish, action.Action.Payload, toolRes.Body);

            await _events.PublishAsync(route.Publish.Subject, evt, ct);
        }

        return new ActionResultV1("action.result.v1", UiPatch: null, Text: null);
    }
}