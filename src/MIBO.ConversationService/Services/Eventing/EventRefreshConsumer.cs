// /backend/src/Infrastructure/Eventing/EventRefreshConsumer.cs

using System.Text.Json;
using MIBO.ConversationService.DTOs.Tools;
using MIBO.ConversationService.Hubs;
using MIBO.ConversationService.Services.Tools;
using MIBO.ConversationService.Services.UI.PatchBuilder;
using MIBO.Storage.Mongo.Store.UiSubscription;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;

namespace MIBO.ConversationService.Services.Eventing;

public sealed class EventRefreshConsumer
{
    private readonly IUiSubscriptionStore _subs;
    private readonly IToolExecutor _tools;
    private readonly IHubContext<UiHub, IUiClient> _hub;

    public EventRefreshConsumer(IUiSubscriptionStore subs, IToolExecutor tools, IHubContext<UiHub, IUiClient> hub)
    {
        _subs = subs;
        _tools = tools;
        _hub = hub;
    }

    public async Task HandleAsync(string subject, string json, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var convId = root.TryGetProperty("conversationId", out var c) ? c.GetString() : null;
        var userId = root.TryGetProperty("userId", out var u) ? u.GetString() : null;

        // 1) Find affected ui instances (global)
        var affected = await _subs.FindAffectedAsync(subject, convId, userId, ct);

        foreach (var inst in affected)
        {
            // 2) From subscriptions pick ones matching this subject
            var refreshSpecs = ExtractRefreshSpecs(inst.Subscriptions, subject);

            var ops = new List<object>();

            foreach (var rs in refreshSpecs)
            {
                var res = await _tools.ExecuteAsync(new ToolCall(rs.Tool, rs.Args), ct);

                var obj = JsonSerializer.Deserialize<object>(
                    res.Body.GetRawText(),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)
                );

                ops.Add(UiPatchBuilder.Replace(rs.PatchPath, obj));
            }

            if (ops.Count == 0) continue;

            var patch = UiPatchBuilder.Patch(ops.ToArray());

            // scope: conversation group
            await _hub.Clients.Group($"conversation:{inst.ConversationId}").UiPatch(patch);
        }
    }

    private sealed record RefreshSpec(string Tool, Dictionary<string, object?> Args, string PatchPath);

    private static List<RefreshSpec> ExtractRefreshSpecs(List<BsonDocument> subs, string subject)
    {
        var list = new List<RefreshSpec>();

        foreach (var sub in subs)
        {
            if (!sub.TryGetValue("event", out var ev) || ev.AsString != subject) continue;
            if (!sub.TryGetValue("refresh", out var rf) || !rf.IsBsonArray) continue;

            foreach (var item in rf.AsBsonArray)
            {
                if (!item.IsBsonDocument) continue;
                var d = item.AsBsonDocument;

                var tool = d.GetValue("tool", "").AsString;
                var patchPath = d.GetValue("patchPath", "").AsString;
                if (string.IsNullOrWhiteSpace(tool) || string.IsNullOrWhiteSpace(patchPath)) continue;

                var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                if (d.TryGetValue("args", out var a) && a.IsBsonDocument)
                {
                    foreach (var el in a.AsBsonDocument.Elements)
                        args[el.Name] = BsonToDotNet(el.Value);
                }

                list.Add(new RefreshSpec(tool, args, patchPath));
            }
        }

        return list;
    }

    private static object? BsonToDotNet(BsonValue v)
        => v.BsonType switch
        {
            BsonType.String => v.AsString,
            BsonType.Int32 => v.AsInt32,
            BsonType.Int64 => v.AsInt64,
            BsonType.Double => v.AsDouble,
            BsonType.Decimal128 => (decimal)v.AsDecimal128,
            BsonType.Boolean => v.AsBoolean,
            BsonType.Null => null,
            _ => v.ToString()
        };
}