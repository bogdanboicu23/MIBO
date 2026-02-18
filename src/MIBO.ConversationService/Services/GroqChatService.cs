using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MIBO.ConversationService.Services;

public class GroqChatService : IGroqChatService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public GroqChatService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(
    string message,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    var apiKey = _config["Groq:ApiKey"];

    var body = new
    {
        model = _config["Groq:Model"] ?? "llama-3.3-70b-versatile",
        stream = true,
        messages = new[]
        {
            new { role = "user", content = message }
        }
    };

    using var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
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

}