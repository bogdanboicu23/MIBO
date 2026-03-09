using System.Text.Json;
using System.Runtime.CompilerServices;
using MIBO.ConversationService.Services.GroqChat;

namespace MIBO.ConversationService.Services.Answer;

public sealed class GroqAnswerService : IAnswerService
{
    private readonly IGroqChatService _groq;
    private readonly ILogger<GroqAnswerService> _logger;

    public GroqAnswerService(IGroqChatService groq, ILogger<GroqAnswerService> logger)
    {
        _groq = groq;
        _logger = logger;
    }

    public async Task<string> AnswerAsync(
        string conversationId,
        string userId,
        string userPrompt,
        object conversationContext,
        CancellationToken ct)
    {
        var prompt = BuildContextAwarePrompt(
            conversationId,
            userId,
            userPrompt,
            conversationContext
        );

        try
        {
            return await _groq.SendMessageAsync(prompt, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Groq answer timeout for conversationId={ConversationId}", conversationId);
            return "Serviciul AI este temporar indisponibil. UI-ul și datele din tool-uri rămân disponibile.";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Groq answer HTTP error for conversationId={ConversationId}, statusCode={StatusCode}",
                conversationId,
                ex.StatusCode
            );
            return "Serviciul AI este temporar indisponibil. Continuă din componentele UI sau reîncearcă în câteva secunde.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq answer failed for conversationId={ConversationId}", conversationId);
            return "Nu am putut genera textul acum, dar poți continua interacțiunea din UI.";
        }
    }

    private static string BuildContextAwarePrompt(
        string conversationId,
        string userId,
        string userPrompt,
        object conversationContext
    )
    {
        var contextJson = CompactConversationContext(conversationContext, maxChars: 2500);

        return $"""
            You are MIBO AI assistant. You must answer using conversation context when relevant.

            Metadata:
            - conversationId: {conversationId}
            - userId: {userId}

            Conversation context (recent):
            {contextJson}

            Current user message:
            {userPrompt}

            Rules:
            1) If user asks about previous messages/questions, use the context above.
            2) Do not claim there is no previous question if previous user messages exist in context.
            3) Keep answer concise, accurate, and grounded in context.
            4) If context is insufficient, explicitly say what is missing.
            """;
    }

    private static string CompactConversationContext(object conversationContext, int maxChars)
    {
        try
        {
            var normalized = NormalizeContext(conversationContext);
            var json = JsonSerializer.Serialize(
                normalized,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
            );
            if (json.Length <= maxChars) return json;
            return json[..maxChars] + "...<truncated>";
        }
        catch
        {
            return "{}";
        }
    }

    private static object NormalizeContext(object conversationContext)
    {
        if (conversationContext is not IReadOnlyDictionary<string, object?> ro)
            return conversationContext;

        var compact = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["conversationId"] = ro.TryGetValue("conversationId", out var c) ? c : null,
            ["userId"] = ro.TryGetValue("userId", out var u) ? u : null,
            ["memorySummary"] = ro.TryGetValue("memorySummary", out var m) ? m : null,
            ["lastMessages"] = CompactMessages(ro.TryGetValue("lastMessages", out var lm) ? lm : null),
            ["hasLastAssistantUi"] = ro.TryGetValue("lastAssistantUi", out var lastUi) && lastUi is not null
        };

        if (ro.TryGetValue("clientContext", out var cc))
            compact["clientContext"] = cc;

        return compact;
    }

    private static object CompactMessages(object? raw)
    {
        if (raw is null) return Array.Empty<object>();

        var messages = new List<Dictionary<string, object?>>();
        foreach (var msg in EnumerateObjects(raw).TakeLast(10))
        {
            var role = ReadValue(msg, "role");
            var text = ReadValue(msg, "text");
            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(text)) continue;

            if (text.Length > 220) text = text[..220] + "...";

            messages.Add(new Dictionary<string, object?>
            {
                ["role"] = role,
                ["text"] = text
            });
        }

        return messages;
    }

    private static IEnumerable<object?> EnumerateObjects(object raw)
    {
        if (raw is IEnumerable<object?> items) return items;

        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Array)
            return je.EnumerateArray().Select(x => (object?)x).ToArray();

        return Array.Empty<object?>();
    }

    private static string ReadValue(object? msg, string prop)
    {
        if (msg is null) return "";

        if (msg is JsonElement je && je.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in je.EnumerateObject())
            {
                if (!string.Equals(p.Name, prop, StringComparison.OrdinalIgnoreCase)) continue;
                return p.Value.ValueKind == JsonValueKind.String
                    ? p.Value.GetString() ?? ""
                    : p.Value.ToString();
            }
            return "";
        }

        if (msg is IReadOnlyDictionary<string, object?> ro)
        {
            if (!ro.TryGetValue(prop, out var value)) return "";
            return Convert.ToString(value) ?? "";
        }

        if (msg is Dictionary<string, object?> dict)
        {
            if (!dict.TryGetValue(prop, out var value)) return "";
            return Convert.ToString(value) ?? "";
        }

        return "";
    }
}

        return await _groq.SendMessageAsync(userPrompt, ct);
    }

    public async IAsyncEnumerable<string> StreamAnswerAsync(
        string conversationId,
        string userId,
        string userPrompt,
        object conversationContext,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var token in _groq.StreamMessageAsync(userPrompt, ct))
        {
            yield return token;
        }
    }
}