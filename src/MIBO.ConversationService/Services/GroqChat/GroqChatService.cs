using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MIBO.ConversationService.Services.GroqChat;

public class GroqChatService : IGroqChatService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly string _baseUrl = "https://api.openai.com/v1/";
    private readonly string? _apiKey;
    private readonly string _model;

    public GroqChatService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
        _apiKey = _config["Groq:ApiKey"];
        _model = _config["Groq:Model"] ?? "llama-3.3-70b-versatile";
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(
    string message,
    [EnumeratorCancellation] CancellationToken ct = default)
{

    var body = new
    {
        model = _model,
        stream = true,
        messages = new[]
        {
            new { role = "user", content = message }
        }
    };

    using var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
    req.Content = new StringContent(
        JsonSerializer.Serialize(body),
        Encoding.UTF8,
        "application/json"
    );

    using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    resp.EnsureSuccessStatusCode();

    await using var stream = await resp.Content.ReadAsStreamAsync(ct);
    using var reader = new StreamReader(stream);

    while (!reader.EndOfStream && !ct.IsCancellationRequested)
    {
        var line = await reader.ReadLineAsync();
        if (line is null) break;

        if (!line.StartsWith("data:")) continue;

        var data = line.Substring("data:".Length).Trim();

        if (data == "[DONE]")
            yield break;

        string? token = null;

        try
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;

            var choices = root.GetProperty("choices");
            if (choices.GetArrayLength() == 0) continue;

            var choice0 = choices[0];

            if (choice0.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("content", out var contentEl))
            {
                token = contentEl.GetString();
            }
        }
        catch
        {
        }

        if (!string.IsNullOrEmpty(token))
        {
            yield return token;
        }
    }
}

    public async Task<string> SendMessageAsync(string message, CancellationToken ct = default)
    {
        var body = new
        {
            model = _model,
            stream = false,
            messages = new[]
            {
                new { role = "user", content = message }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        req.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json"
        );

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var responseBody = await resp.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var choices = root.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
            return string.Empty;

        var choice0 = choices[0];
        if (choice0.TryGetProperty("message", out var messageObj) &&
            messageObj.TryGetProperty("content", out var content))
        {
            return content.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}