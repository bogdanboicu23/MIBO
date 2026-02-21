// /backend/src/Infrastructure/Eventing/EventRefreshHandler.cs

using System.Text.Json;
using MIBO.ConversationService.DTOs.Eventing;
using MIBO.ConversationService.DTOs.Tools;
using MIBO.ConversationService.Helper;
using MIBO.ConversationService.Hubs;
using MIBO.ConversationService.Services.Background;
using MIBO.ConversationService.Services.Tools;
using MIBO.ConversationService.Services.UI.PatchBuilder;
using MIBO.Storage.Mongo.Store.UiSubscription;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;

namespace MIBO.ConversationService.Services.Eventing.Handler;

public sealed class EventRefreshHandler : IEventHandler
{
    private readonly IUiSubscriptionStore _subs;
    private readonly IToolExecutor _tools;
    private readonly IHubContext<UiHub, IUiClient> _hub;
    private readonly SingleFlight _singleFlight; // reuse (dedup)

    public EventRefreshHandler(
        IUiSubscriptionStore subs,
        IToolExecutor tools,
        IHubContext<UiHub, IUiClient> hub,
        SingleFlight singleFlight)
    {
        _subs = subs;
        _tools = tools;
        _hub = hub;
        _singleFlight = singleFlight;
    }

    public async Task HandleAsync(string subject, string json, CancellationToken ct)
    {
        // Parse event envelope (if payload isn't envelope, you can fallback)
        var evt = JsonSerializer.Deserialize<EventEnvelopeV1>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (evt is null || evt.Schema != "event.v1") return;

        // Dedup per (subject + conversationId + userId) for short window
        var dedupKey = $"evt:{evt.Subject}:{evt.ConversationId}:{evt.UserId}";
        await _singleFlight.DoAsync(dedupKey, async () =>
        {
            var affected = await _subs.FindAffectedAsync(evt.Subject, evt.ConversationId, evt.UserId, ct);

            foreach (var inst in affected)
            {
                var refresh = ExtractRefresh(inst.Subscriptions, evt.Subject);
                if (refresh.Count == 0) continue;

                // execute refresh sequential or limited parallel
                var ops = new List<object>();

                foreach (var r in refresh)
                {
                    var res = await _tools.ExecuteAsync(new ToolCall(r.Tool, r.Args), ct);
                    var obj = JsonSerializer.Deserialize<object>(res.Body.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    ops.Add(UiPatchBuilder.Replace(r.PatchPath, obj));
                }

                if (ops.Count == 0) continue;

                var patch = UiPatchBuilder.Patch(ops.ToArray());
                await _hub.Clients.Group($"conversation:{inst.ConversationId}").UiPatch(patch);
            }

            return 0;
        });
    }

    private sealed record RefreshSpec(string Tool, Dictionary<string, object?> Args, string PatchPath);

    private static List<RefreshSpec> ExtractRefresh(List<BsonDocument> subs, string subject)
    {
        var list = new List<RefreshSpec>();

        foreach (var s in subs)
        {
            if (!s.TryGetValue("event", out var ev) || ev.AsString != subject) continue;
            if (!s.TryGetValue("refresh", out var rf) || !rf.IsBsonArray) continue;

            foreach (var item in rf.AsBsonArray)
            {
                if (!item.IsBsonDocument) continue;
                var d = item.AsBsonDocument;

                var tool = d.GetValue("tool", "").AsString;
                var patchPath = d.GetValue("patchPath", "").AsString;
                if (string.IsNullOrWhiteSpace(tool) || string.IsNullOrWhiteSpace(patchPath)) continue;

                var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                if (d.TryGetValue("args", out var a) && a.IsBsonDocument)
                    foreach (var el in a.AsBsonDocument.Elements)
                        args[el.Name] = BsonToDotNet(el.Value);

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