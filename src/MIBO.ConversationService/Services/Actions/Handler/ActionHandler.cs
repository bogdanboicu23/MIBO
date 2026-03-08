using System.Text.Json;
using MIBO.ConversationService.DTOs.Actions;
using MIBO.ConversationService.DTOs.Contracts;
using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.DTOs.Tools;
using MIBO.ConversationService.Services.Actions.Binding;
using MIBO.ConversationService.Services.Actions.Patch;
using MIBO.ConversationService.Services.Actions.Router;
using MIBO.ConversationService.Services.Chat;
using MIBO.ConversationService.Services.Eventing.Builder;
using MIBO.ConversationService.Services.Eventing.Factory;
using MIBO.ConversationService.Services.Eventing.JetStream;
using MIBO.ConversationService.Services.Tools;
using MIBO.Storage.Mongo.Store.Ui;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Actions.Handler;

public sealed class ActionHandler : IActionHandler
{
    private readonly IActionRouter _router;
    private readonly IToolExecutor _tools;
    private readonly IToolRegistry _toolRegistry;
    private readonly IActionArgumentBinder _argumentBinder;
    private readonly IActionPatchComposer _patchComposer;
    private readonly IEventPublisher _events;
    private readonly IEventEnvelopeFactory _envelopeFactory;
    private readonly IEventPayloadBuilder _payloadBuilder;
    private readonly IChatOrchestrator _chat;
    private readonly IUiInstanceStore _uiStore;
    private readonly ChatOrchestratorOptions _opt;
    private readonly ILogger<ActionHandler> _logger;

    public ActionHandler(
        IActionRouter router,
        IToolExecutor tools,
        IToolRegistry toolRegistry,
        IActionArgumentBinder argumentBinder,
        IActionPatchComposer patchComposer,
        IEventPublisher events,
        IEventEnvelopeFactory envelopeFactory,
        IEventPayloadBuilder payloadBuilder,
        IChatOrchestrator chat,
        IUiInstanceStore uiStore,
        IOptions<ChatOrchestratorOptions> opt,
        ILogger<ActionHandler> logger
    )
    {
        _router = router;
        _tools = tools;
        _toolRegistry = toolRegistry;
        _argumentBinder = argumentBinder;
        _patchComposer = patchComposer;
        _events = events;
        _envelopeFactory = envelopeFactory;
        _payloadBuilder = payloadBuilder;
        _chat = chat;
        _uiStore = uiStore;
        _opt = opt.Value;
        _logger = logger;
    }

    public async Task<ActionResultV1> HandleAsync(ActionEnvelopeV1 action, CancellationToken ct)
    {
        var route = await _router.TryResolveAsync(action.Action.Type, ct);
        if (route is null)
        {
            return new ActionResultV1(
                Schema: "action.result.v1",
                UiPatch: null,
                Text: $"Action '{action.Action.Type}' is not registered.",
                Warnings: new[] { "action_route_not_found" }
            );
        }

        var mode = (route.Mode ?? "tool").Trim().ToLowerInvariant();
        if (mode == "prompt")
            return await HandlePromptRouteAsync(route, action, ct);

        return await HandleToolRouteAsync(route, action, ct);
    }

    private async Task<ActionResultV1> HandlePromptRouteAsync(
        ActionRoute route,
        ActionEnvelopeV1 action,
        CancellationToken ct
    )
    {
        var prompt = RenderPromptTemplate(route.PromptTemplate, action.Action.Payload);
        if (string.IsNullOrWhiteSpace(prompt))
            prompt = $"Handle action '{action.Action.Type}' with payload context.";

        var response = await _chat.HandleAsync(
            new ChatRequest(
                action.ConversationId,
                action.UserId,
                prompt,
                ClientContext: new Dictionary<string, object?>
                {
                    ["source"] = "ui.action",
                    ["actionType"] = action.Action.Type,
                    ["actionPayload"] = action.Action.Payload,
                    ["uiContext"] = action.UiContext
                }
            ),
            ct
        );

        object? uiPatch = null;
        if (_opt.PreferInPlaceForActions && action.UiContext?.UiInstanceId is not null && response.UiV1 is not null)
        {
            uiPatch = BuildReplaceUiPatch(response.UiV1, action.UiContext.UiInstanceId);
            await _uiStore.SavePatchAsync(action.UiContext.UiInstanceId, uiPatch, ct);
        }

        return new ActionResultV1(
            Schema: "action.result.v1",
            UiPatch: uiPatch,
            Text: response.Text,
            UiV1: response.UiV1,
            CorrelationId: response.CorrelationId,
            Warnings: response.Warnings
        );
    }

    private async Task<ActionResultV1> HandleToolRouteAsync(
        ActionRoute route,
        ActionEnvelopeV1 action,
        CancellationToken ct
    )
    {
        var selectedTool = SelectTool(route, action.Action.Payload);
        if (string.IsNullOrWhiteSpace(selectedTool))
        {
            return new ActionResultV1(
                Schema: "action.result.v1",
                UiPatch: null,
                Text: $"Route '{action.Action.Type}' has no tool configured.",
                Warnings: new[] { "tool_route_missing_tool" }
            );
        }

        if (!_toolRegistry.TryGet(selectedTool, out var toolDefinition) || toolDefinition is null)
        {
            return new ActionResultV1(
                Schema: "action.result.v1",
                UiPatch: null,
                Text: $"Tool '{selectedTool}' is not available.",
                Warnings: new[] { "tool_not_found" }
            );
        }

        var args = _argumentBinder.Bind(route, action, toolDefinition);
        ToolResult toolResult;
        try
        {
            toolResult = await _tools.ExecuteAsync(new ToolCall(selectedTool, args), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Action tool execution failed for actionType={ActionType}, tool={Tool}", action.Action.Type, selectedTool);
            return new ActionResultV1(
                Schema: "action.result.v1",
                UiPatch: null,
                Text: "Acțiunea nu a putut fi executată în acest moment.",
                Warnings: new[] { "tool_execution_exception" }
            );
        }

        if (toolResult.StatusCode is not (>= 200 and < 300))
        {
            return new ActionResultV1(
                Schema: "action.result.v1",
                UiPatch: null,
                Text: $"Action failed with status {toolResult.StatusCode}.",
                Warnings: new[] { "tool_http_error" }
            );
        }

        var toolBody = JsonSerializer.Deserialize<object>(
            toolResult.Body.GetRawText(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        );

        if (route.Publish is not null)
        {
            var evt = _envelopeFactory.CreateBase(route.Publish.Subject, action.ConversationId, action.UserId);
            evt.Payload = _payloadBuilder.Build(route.Publish, action.Action.Payload, toolResult.Body);
            await _events.PublishAsync(route.Publish.Subject, evt, ct);
        }

        var patch = _patchComposer.ComposeDataPatch(route, selectedTool, toolBody, action);
        if (action.UiContext?.UiInstanceId is not null)
            await _uiStore.SavePatchAsync(action.UiContext.UiInstanceId, patch, ct);

        var toolStates = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [selectedTool] = new
            {
                status = "ok",
                tool = selectedTool,
                updatedAtUtc = DateTime.UtcNow
            }
        };

        return new ActionResultV1(
            Schema: "action.result.v1",
            UiPatch: patch,
            Text: route.SuccessText,
            ToolStates: toolStates
        );
    }

    private static string SelectTool(ActionRoute route, IReadOnlyDictionary<string, object?> payload)
    {
        if (route.ToolCandidates.Count == 0)
            return route.Tool;

        foreach (var candidate in route.ToolCandidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.Tool)) continue;
            if (!MatchesCandidate(candidate, payload)) continue;
            return candidate.Tool;
        }

        return route.Tool;
    }

    private static bool MatchesCandidate(
        ActionToolCandidate candidate,
        IReadOnlyDictionary<string, object?> payload
    )
    {
        if (candidate.WhenPayloadHasAll.Any(key => !PayloadHasValue(payload, key)))
            return false;

        if (candidate.WhenPayloadHasAny.Count > 0 &&
            !candidate.WhenPayloadHasAny.Any(key => PayloadHasValue(payload, key)))
            return false;

        foreach (var (k, v) in candidate.WhenPayloadEquals)
        {
            if (!TryGetPayloadValue(payload, k, out var current)) return false;
            if (!string.Equals(Convert.ToString(current), v, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static bool PayloadHasValue(IReadOnlyDictionary<string, object?> payload, string keyPath)
    {
        return TryGetPayloadValue(payload, keyPath, out var value) &&
               !string.IsNullOrWhiteSpace(Convert.ToString(value));
    }

    private static bool TryGetPayloadValue(
        IReadOnlyDictionary<string, object?> payload,
        string keyPath,
        out object? value
    )
    {
        value = null;
        if (string.IsNullOrWhiteSpace(keyPath)) return false;

        object? current = payload;
        foreach (var part in keyPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current is IReadOnlyDictionary<string, object?> ro)
            {
                if (!ro.TryGetValue(part, out current))
                    return false;
                continue;
            }

            if (current is Dictionary<string, object?> dict)
            {
                if (!dict.TryGetValue(part, out current))
                    return false;
                continue;
            }

            return false;
        }

        value = current;
        return true;
    }

    private static string RenderPromptTemplate(string? template, IReadOnlyDictionary<string, object?> payload)
    {
        var value = template ?? "";
        if (string.IsNullOrWhiteSpace(value)) return "";

        foreach (var (k, v) in payload)
        {
            var replacement = Convert.ToString(v) ?? "";
            value = value.Replace("{{" + k + "}}", replacement, StringComparison.OrdinalIgnoreCase);
            value = value.Replace("{{payload." + k + "}}", replacement, StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }

    private static object BuildReplaceUiPatch(object uiV1, string uiInstanceId)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(uiV1, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var root = doc.RootElement;

        var ops = new List<object>();

        if (root.TryGetProperty("root", out var rootNode))
            ops.Add(Services.UI.PatchBuilder.UiPatchBuilder.Replace(
                "/root",
                JsonSerializer.Deserialize<object>(rootNode.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ));

        if (root.TryGetProperty("data", out var data))
            ops.Add(Services.UI.PatchBuilder.UiPatchBuilder.Replace(
                "/data",
                JsonSerializer.Deserialize<object>(data.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ));

        if (root.TryGetProperty("bindings", out var bindings))
            ops.Add(Services.UI.PatchBuilder.UiPatchBuilder.Replace(
                "/bindings",
                JsonSerializer.Deserialize<object>(bindings.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ));

        if (root.TryGetProperty("subscriptions", out var subscriptions))
            ops.Add(Services.UI.PatchBuilder.UiPatchBuilder.Replace(
                "/subscriptions",
                JsonSerializer.Deserialize<object>(subscriptions.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ));

        return Services.UI.PatchBuilder.UiPatchBuilder.Patch(
            uiInstanceId: uiInstanceId,
            ops: ops.ToArray()
        );
    }
}
