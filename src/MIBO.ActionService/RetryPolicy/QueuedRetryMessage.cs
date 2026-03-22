using System.Text.Json;

namespace MIBO.ActionService.RetryPolicy;

public sealed record QueuedRetryMessage
{
    public string ServiceKey { get; init; } = string.Empty;
    public string Handler { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public string ArgsJson { get; init; } = "{}";
    public int Attempt { get; init; } = 1;
    public int MaxAttempts { get; init; } = 1;
    public string InitialOrigin { get; init; } = ExternalServiceExecutionOrigins.Interactive;
    public DateTime EnqueuedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? NextAttemptAtUtc { get; init; }

    public IReadOnlyDictionary<string, object?> ToArguments()
    {
        if (string.IsNullOrWhiteSpace(ArgsJson))
        {
            return new Dictionary<string, object?>();
        }

        using var document = JsonDocument.Parse(ArgsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, object?>();
        }

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            result[property.Name] = ConvertElement(property.Value);
        }

        return result;
    }

    public static string SerializeArguments(IReadOnlyDictionary<string, object?> args)
    {
        return JsonSerializer.Serialize(args);
    }

    private static object? ConvertElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var int64Value) => int64Value,
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertElement).ToArray(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(property => property.Name, property => ConvertElement(property.Value), StringComparer.OrdinalIgnoreCase),
            _ => element.ToString(),
        };
    }
}
