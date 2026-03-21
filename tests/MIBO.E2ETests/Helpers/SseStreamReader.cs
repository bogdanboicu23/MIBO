using System.Text.Json;

namespace MIBO.E2ETests.Helpers;

public static class SseStreamReader
{
    public static async Task<List<SseEvent>> ReadEventsAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        var events = new List<SseEvent>();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? dataBuffer = null;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;

            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                dataBuffer = line[6..];
            }
            else if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                dataBuffer = line[5..];
            }
            else if (line == string.Empty && dataBuffer is not null)
            {
                events.Add(ParseEvent(dataBuffer));
                dataBuffer = null;
            }
        }

        if (dataBuffer is not null)
        {
            events.Add(ParseEvent(dataBuffer));
        }

        return events;
    }

    private static SseEvent ParseEvent(string data)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            var type = doc.RootElement.TryGetProperty("type", out var t)
                ? t.GetString() ?? "unknown"
                : "unknown";
            return new SseEvent(type, data);
        }
        catch (JsonException)
        {
            return new SseEvent("raw", data);
        }
    }
}

public record SseEvent(string Type, string RawData);
