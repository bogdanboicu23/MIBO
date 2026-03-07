using MIBO.ConversationService.DTOs.Actions;
using MIBO.ConversationService.DTOs.Contracts;
using MIBO.ConversationService.Services.Chat;
using MIBO.ConversationService.DTOs.Tools;
using MIBO.ConversationService.Services.Actions.Router;
using MIBO.ConversationService.Services.Eventing;
using MIBO.ConversationService.Services.Eventing.Builder;
using MIBO.ConversationService.Services.Eventing.Factory;
using MIBO.ConversationService.Services.Eventing.JetStream;
using MIBO.ConversationService.Services.Tools;
using MIBO.ConversationService.Services.UI.PatchBuilder;

namespace MIBO.ConversationService.Services.Actions.Handler;

public sealed class ActionHandler : IActionHandler
{
    private readonly IActionRouter _router;
    private readonly IToolExecutor _tools;
    private readonly IEventPublisher _events;
    private readonly IEventEnvelopeFactory _envelopeFactory;
    private readonly IEventPayloadBuilder _payloadBuilder;
    private readonly IChatOrchestrator _chat;

    public ActionHandler(
        IActionRouter router,
        IToolExecutor tools,
        IEventPublisher events,
        IEventEnvelopeFactory envelopeFactory,
        IEventPayloadBuilder payloadBuilder,
        IChatOrchestrator chat)
    {
        _router = router;
        _tools = tools;
        _events = events;
        _envelopeFactory = envelopeFactory;
        _payloadBuilder = payloadBuilder;
        _chat = chat;
    }

    public async Task<ActionResultV1> HandleAsync(ActionEnvelopeV1 action, CancellationToken ct)
    {
        var route = await _router.ResolveAsync(action.Action.Type, ct);
        var mode = (route.Mode ?? "tool").Trim().ToLowerInvariant();

        // UI interactive actions should update current UI via patch when possible.
        var deterministic = await TryHandleDeterministicUiActionAsync(action, ct);
        if (deterministic is not null) return deterministic;

        if (mode == "prompt")
        {
            var prompt = RenderPromptTemplate(route.PromptTemplate, action.Action.Payload);
            if (string.IsNullOrWhiteSpace(prompt))
                prompt = $"Handle action '{action.Action.Type}' with payload context.";

            var chatResponse = await _chat.HandleAsync(
                new ChatRequest(
                    action.ConversationId,
                    action.UserId,
                    prompt,
                    ClientContext: new Dictionary<string, object?>
                    {
                        ["source"] = "ui.action",
                        ["actionType"] = action.Action.Type,
                        ["actionPayload"] = action.Action.Payload,
                    }
                ),
                ct
            );

            return new ActionResultV1(
                Schema: "action.result.v1",
                UiPatch: null,
                Text: chatResponse.Text,
                UiV1: chatResponse.UiV1,
                CorrelationId: chatResponse.CorrelationId
            );
        }

        if (string.IsNullOrWhiteSpace(route.Tool))
        {
            return new ActionResultV1(
                Schema: "action.result.v1",
                UiPatch: null,
                Text: $"Route '{action.Action.Type}' has no tool configured."
            );
        }

        var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (route.ArgMap.Count == 0)
        {
            foreach (var (k, v) in action.Action.Payload)
            {
                if (string.Equals(k, "__ui", StringComparison.OrdinalIgnoreCase)) continue;
                args[k] = v;
            }
        }
        else
        {
            foreach (var (payloadKey, toolArgKey) in route.ArgMap)
            {
                action.Action.Payload.TryGetValue(payloadKey, out var val);
                args[toolArgKey] = val;
            }
        }

        var toolRes = await _tools.ExecuteAsync(new ToolCall(route.Tool, args), ct);
        var toolBody = System.Text.Json.JsonSerializer.Deserialize<object>(
            toolRes.Body.GetRawText(),
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
        );

        if (route.Publish is not null && toolRes.StatusCode is >= 200 and < 300)
        {
            var evt = _envelopeFactory.CreateBase(route.Publish.Subject, action.ConversationId, action.UserId);
            evt.Payload = _payloadBuilder.Build(route.Publish, action.Action.Payload, toolRes.Body);

            await _events.PublishAsync(route.Publish.Subject, evt, ct);
        }

        if (toolRes.StatusCode is >= 200 and < 300)
        {
            var patch = UiPatchBuilder.Patch(
                UiPatchBuilder.Replace($"/data/{route.Tool}", toolBody)
            );
            return new ActionResultV1("action.result.v1", UiPatch: patch, Text: null);
        }

        return new ActionResultV1("action.result.v1", UiPatch: null, Text: $"Action failed with status {toolRes.StatusCode}.");
    }

    private async Task<ActionResultV1?> TryHandleDeterministicUiActionAsync(ActionEnvelopeV1 action, CancellationToken ct)
    {
        var actionType = action.Action.Type?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(actionType)) return null;

        string? tool = null;
        var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        switch (actionType)
        {
            case "shop.search":
            {
                var q = GetPayloadString(action.Action.Payload, "query")
                        ?? GetPayloadString(action.Action.Payload, "q");
                if (string.IsNullOrWhiteSpace(q)) return null;
                tool = "shop.searchProducts";
                args["q"] = q;
                args["skip"] = GetPayloadNumber(action.Action.Payload, "skip") ?? 0;
                args["limit"] = GetPayloadNumber(action.Action.Payload, "limit") ?? 10;
                break;
            }
            case "shop.select_category":
            {
                var category = GetPayloadString(action.Action.Payload, "category")
                               ?? GetPayloadString(action.Action.Payload, "slug");
                if (string.IsNullOrWhiteSpace(category)) return null;
                tool = "shop.listProductsByCategory";
                args["category"] = category;
                args["skip"] = GetPayloadNumber(action.Action.Payload, "skip") ?? 0;
                args["limit"] = GetPayloadNumber(action.Action.Payload, "limit") ?? 10;
                break;
            }
            case "shop.change_page":
            {
                var q = GetPayloadString(action.Action.Payload, "query")
                        ?? GetPayloadString(action.Action.Payload, "q");
                var category = GetPayloadString(action.Action.Payload, "category")
                               ?? GetPayloadString(action.Action.Payload, "slug");
                if (!string.IsNullOrWhiteSpace(q))
                {
                    tool = "shop.searchProducts";
                    args["q"] = q;
                }
                else if (!string.IsNullOrWhiteSpace(category))
                {
                    tool = "shop.listProductsByCategory";
                    args["category"] = category;
                }
                else
                {
                    tool = "shop.listProducts";
                }
                args["skip"] = GetPayloadNumber(action.Action.Payload, "skip") ?? 0;
                args["limit"] = GetPayloadNumber(action.Action.Payload, "limit") ?? 10;
                break;
            }
            case "shop.sort":
            {
                tool = "shop.listProductsSorted";
                var sortBy = GetPayloadString(action.Action.Payload, "sortBy");
                var order = GetPayloadString(action.Action.Payload, "order");
                var sort = GetPayloadString(action.Action.Payload, "sort");

                if (string.IsNullOrWhiteSpace(sortBy) && !string.IsNullOrWhiteSpace(sort))
                {
                    // Accept shorthand: "price_desc" / "price:desc"
                    var normalized = sort!.Replace(":", "_");
                    var parts = normalized.Split('_', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        sortBy = parts[0];
                        order = parts[1];
                    }
                }

                args["sortBy"] = string.IsNullOrWhiteSpace(sortBy) ? "rating" : sortBy;
                args["order"] = string.IsNullOrWhiteSpace(order) ? "desc" : order;
                args["skip"] = GetPayloadNumber(action.Action.Payload, "skip") ?? 0;
                args["limit"] = GetPayloadNumber(action.Action.Payload, "limit") ?? 10;
                break;
            }
            case "shop.open_product":
            {
                var id = GetPayloadNumber(action.Action.Payload, "productId")
                         ?? GetPayloadNumber(action.Action.Payload, "id");
                if (id is null) return null;
                tool = "shop.getProduct";
                args["id"] = id.Value;
                break;
            }
            default:
                return null;
        }

        if (string.IsNullOrWhiteSpace(tool)) return null;

        var toolRes = await _tools.ExecuteAsync(new ToolCall(tool, args), ct);
        if (toolRes.StatusCode is not (>= 200 and < 300))
            return new ActionResultV1("action.result.v1", UiPatch: null, Text: $"Action failed with status {toolRes.StatusCode}.");

        var obj = System.Text.Json.JsonSerializer.Deserialize<object>(
            toolRes.Body.GetRawText(),
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
        );

        var ops = new List<object> { UiPatchBuilder.Replace($"/data/{tool}", obj) };

        // Keep common aliases in sync to maximize compatibility with generated dataKey values.
        if (tool == "shop.searchProducts")
            ops.Add(UiPatchBuilder.Replace("/data/shop.listProducts", obj));
        if (tool == "shop.listProducts")
            ops.Add(UiPatchBuilder.Replace("/data/shop.searchProducts", obj));

        var patch = UiPatchBuilder.Patch(ops.ToArray());
        return new ActionResultV1("action.result.v1", UiPatch: patch, Text: null);
    }

    private static string? GetPayloadString(IReadOnlyDictionary<string, object?> payload, string key)
    {
        if (!payload.TryGetValue(key, out var v) || v is null) return null;
        var s = Convert.ToString(v)?.Trim();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static int? GetPayloadNumber(IReadOnlyDictionary<string, object?> payload, string key)
    {
        if (!payload.TryGetValue(key, out var v) || v is null) return null;
        if (v is int i) return i;
        if (v is long l && l <= int.MaxValue && l >= int.MinValue) return (int)l;
        if (int.TryParse(Convert.ToString(v), out var parsed)) return parsed;
        return null;
    }

    private static string RenderPromptTemplate(string? template, IReadOnlyDictionary<string, object?> payload)
    {
        var value = template ?? "";
        if (string.IsNullOrWhiteSpace(value)) return "";

        foreach (var (k, v) in payload)
        {
            var replacement = Convert.ToString(v) ?? "";
            value = value.Replace("{{" + k + "}}", replacement, StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }
}
