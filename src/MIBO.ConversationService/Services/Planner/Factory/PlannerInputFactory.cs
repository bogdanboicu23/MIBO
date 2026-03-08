using System.Text.Json;
using MIBO.ConversationService.DTOs.PlannerContracts;
using MIBO.ConversationService.Middleware.Http;
using MIBO.ConversationService.Services.Tools;
using MIBO.ConversationService.Services.UI;
using Microsoft.Extensions.Options;

namespace MIBO.ConversationService.Services.Planner.Factory;

public sealed class PlannerOptions
{
    public int MaxToolSteps { get; set; } = 8;

    public int MaxContextChars { get; set; } = 6_000;

    public int MaxToolsInCatalog { get; set; } = 80;
}

public sealed class PlannerInputFactory : IPlannerInputFactory
{
    private readonly IToolRegistry _tools;
    private readonly IUiCatalogProvider _uiCatalog;
    private readonly IHeaderContextAccessor _headers;
    private readonly PlannerOptions _opt;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public PlannerInputFactory(
        IToolRegistry tools,
        IUiCatalogProvider uiCatalog,
        IHeaderContextAccessor headers,
        IOptions<PlannerOptions> opt
    )
    {
        _tools = tools;
        _uiCatalog = uiCatalog;
        _headers = headers;
        _opt = opt.Value;
    }

    public async Task<PlannerInputV1> BuildAsync(
        string conversationId,
        string userId,
        string userPrompt,
        Dictionary<string, object?> conversationContext,
        CancellationToken ct
    )
    {
        var tools = _tools.All()
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, _opt.MaxToolsInCatalog))
            .Select(t => new PlannerToolDescriptor
            {
                Name = t.Name,
                Description = t.Description,
                Method = t.Method,
                RequiredArgs = t.RequiredArgs,
                DefaultArgs = new Dictionary<string, object?>(t.DefaultArgs, StringComparer.OrdinalIgnoreCase)
            })
            .ToList();

        var uiComponentCatalog = await _uiCatalog.GetCatalogAsync(ct);
        var compactContext = CompactContext(conversationContext, _opt.MaxContextChars);
        var headerContext = _headers.Get();

        return new PlannerInputV1
        {
            Schema = "planner_input.v1",
            UserPrompt = userPrompt,
            ConversationContext = compactContext,
            ToolCatalog = new PlannerToolCatalog { Tools = tools },
            UiComponentCatalog = uiComponentCatalog,
            Constraints = new PlannerInputConstraints { MaxSteps = _opt.MaxToolSteps },
            Meta = new PlannerInputMeta
            {
                ConversationId = conversationId,
                UserId = userId,
                CorrelationId = headerContext.CorrelationId
            }
        };
    }

    private static Dictionary<string, object?> CompactContext(
        Dictionary<string, object?> context,
        int maxChars
    )
    {
        var normalized = new Dictionary<string, object?>(context, StringComparer.OrdinalIgnoreCase);
        var serialized = JsonSerializer.Serialize(normalized, JsonOpts);
        if (serialized.Length <= maxChars) return normalized;

        var compact = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        compact["conversationId"] = normalized.GetValueOrDefault("conversationId");
        compact["userId"] = normalized.GetValueOrDefault("userId");

        var memorySummary = Convert.ToString(normalized.GetValueOrDefault("memorySummary")) ?? "";
        if (memorySummary.Length > 0)
            compact["memorySummary"] = memorySummary.Length <= 1_200
                ? memorySummary
                : memorySummary[..1_200];

        if (normalized.TryGetValue("lastMessages", out var lastMessagesRaw))
            compact["lastMessages"] = CompactMessages(lastMessagesRaw);

        if (normalized.TryGetValue("lastAssistantUi", out var lastUi))
            compact["lastAssistantUi"] = CompactUiSnapshot(lastUi);

        if (normalized.TryGetValue("clientContext", out var clientContext))
            compact["clientContext"] = clientContext;

        if (normalized.TryGetValue("externalContextUrls", out var externalContextUrls))
            compact["externalContextUrls"] = externalContextUrls;

        serialized = JsonSerializer.Serialize(compact, JsonOpts);
        if (serialized.Length <= maxChars) return compact;

        // Final hard cap to protect planner token cost.
        compact["memorySummary"] = (Convert.ToString(compact.GetValueOrDefault("memorySummary")) ?? "")[..Math.Min(600, (Convert.ToString(compact.GetValueOrDefault("memorySummary")) ?? "").Length)];
        compact["lastMessages"] = CompactMessages(compact.GetValueOrDefault("lastMessages"), maxMessages: 4, maxTextChars: 180);
        return compact;
    }

    private static object CompactMessages(object? raw, int maxMessages = 8, int maxTextChars = 300)
    {
        if (raw is not IEnumerable<object?> items) return Array.Empty<object>();

        var list = items
            .TakeLast(maxMessages)
            .Select(x =>
            {
                if (x is not IDictionary<string, object?> m)
                    return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                m.TryGetValue("text", out var textRaw);
                var text = Convert.ToString(textRaw) ?? "";
                if (text.Length > maxTextChars) text = text[..maxTextChars];

                m.TryGetValue("role", out var roleRaw);
                m.TryGetValue("createdAt", out var createdAtRaw);

                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["role"] = Convert.ToString(roleRaw) ?? "",
                    ["text"] = text,
                    ["createdAt"] = createdAtRaw
                };
            })
            .ToArray();

        return list;
    }

    private static object? CompactUiSnapshot(object? rawUi)
    {
        if (rawUi is null) return null;

        try
        {
            var uiDoc = JsonDocument.Parse(JsonSerializer.Serialize(rawUi, JsonOpts));
            if (uiDoc.RootElement.ValueKind != JsonValueKind.Object) return null;

            var root = uiDoc.RootElement.Clone();
            var compact = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["schema"] = root.TryGetProperty("schema", out var s) ? s.GetString() : "ui.v1",
                ["root"] = root.TryGetProperty("root", out var r)
                    ? JsonSerializer.Deserialize<object>(r.GetRawText(), JsonOpts)
                    : null
            };

            if (root.TryGetProperty("bindings", out var bindings))
                compact["bindings"] = JsonSerializer.Deserialize<object>(bindings.GetRawText(), JsonOpts);

            if (root.TryGetProperty("subscriptions", out var subscriptions))
                compact["subscriptions"] = JsonSerializer.Deserialize<object>(subscriptions.GetRawText(), JsonOpts);

            return compact;
        }
        catch
        {
            return null;
        }
    }
}
