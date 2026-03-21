using System.Globalization;
using System.Text.Json;
using MIBO.ActionService.ExternalServices.Abstractions;
using MIBO.ActionService.Services;

namespace MIBO.ActionService.ExternalServices.OpenWeatherMap;

public sealed class OpenWeatherMapActionHandler(IOpenWeatherMapClient client) : IExternalDataSourceHandler
{
    private static readonly HashSet<string> SupportedHandlers = new(StringComparer.OrdinalIgnoreCase)
    {
        "weather.current.get",
        "weather.forecast.get",
    };

    public bool CanHandle(string handler)
    {
        return !string.IsNullOrWhiteSpace(handler) && SupportedHandlers.Contains(handler);
    }

    public Task<object> QueryAsync(
        string handler,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        return handler switch
        {
            "weather.current.get" => GetCurrentWeatherAsync(args, cancellationToken),
            "weather.forecast.get" => GetForecastAsync(args, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported OpenWeatherMap handler '{handler}'."),
        };
    }

    public DataFieldHints GetFieldHints(string handler)
    {
        return handler switch
        {
            "weather.current.get" => new DataFieldHints
            {
                EntityType = "weather_current",
                LabelField = "city",
                ValueField = "temp",
                TitleField = "city",
                ImageField = "iconUrl",
                SearchField = "city",
            },
            "weather.forecast.get" => new DataFieldHints
            {
                EntityType = "weather_forecast",
                CollectionPath = "items",
                LabelField = "date",
                ValueField = "temp",
                TitleField = "city",
                ImageField = "iconUrl",
                SearchField = "city",
            },
            _ => new DataFieldHints(),
        };
    }

    private async Task<object> GetCurrentWeatherAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var city = GetString(args, "city", "q");
        if (string.IsNullOrWhiteSpace(city))
        {
            throw new InvalidOperationException("weather.current.get requires a city argument.");
        }

        var path = $"/data/2.5/weather?q={Uri.EscapeDataString(city)}";
        var payload = await client.GetAsync(path, cancellationToken);

        return MapCurrentWeather(payload);
    }

    private async Task<object> GetForecastAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var city = GetString(args, "city", "q");
        if (string.IsNullOrWhiteSpace(city))
        {
            throw new InvalidOperationException("weather.forecast.get requires a city argument.");
        }

        var path = $"/data/2.5/forecast?q={Uri.EscapeDataString(city)}";
        var payload = await client.GetAsync(path, cancellationToken);

        return MapForecast(payload);
    }

    private static object MapCurrentWeather(JsonElement payload)
    {
        var weather = GetFirstWeather(payload);

        return new Dictionary<string, object?>
        {
            ["city"] = GetString(payload, "name"),
            ["country"] = GetNestedString(payload, "sys", "country"),
            ["temp"] = GetDouble(payload, "main", "temp"),
            ["feelsLike"] = GetDouble(payload, "main", "feels_like"),
            ["tempMin"] = GetDouble(payload, "main", "temp_min"),
            ["tempMax"] = GetDouble(payload, "main", "temp_max"),
            ["humidity"] = GetInt(payload, "main", "humidity"),
            ["pressure"] = GetInt(payload, "main", "pressure"),
            ["windSpeed"] = GetDouble(payload, "wind", "speed"),
            ["windDeg"] = GetInt(payload, "wind", "deg"),
            ["description"] = weather.description,
            ["icon"] = weather.icon,
            ["iconUrl"] = BuildIconUrl(weather.icon),
            ["clouds"] = GetInt(payload, "clouds", "all"),
            ["visibility"] = GetOptionalInt(payload, "visibility"),
            ["sunrise"] = GetOptionalLong(payload, "sys", "sunrise"),
            ["sunset"] = GetOptionalLong(payload, "sys", "sunset"),
        };
    }

    private static object MapForecast(JsonElement payload)
    {
        var cityName = GetNestedString(payload, "city", "name");
        var country = GetNestedString(payload, "city", "country");

        var items = new List<Dictionary<string, object?>>();

        if (payload.TryGetProperty("list", out var listElement) && listElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in listElement.EnumerateArray())
            {
                var weather = GetFirstWeather(entry);

                items.Add(new Dictionary<string, object?>
                {
                    ["dt"] = GetOptionalLong(entry, "dt"),
                    ["date"] = GetString(entry, "dt_txt"),
                    ["temp"] = GetDouble(entry, "main", "temp"),
                    ["feelsLike"] = GetDouble(entry, "main", "feels_like"),
                    ["tempMin"] = GetDouble(entry, "main", "temp_min"),
                    ["tempMax"] = GetDouble(entry, "main", "temp_max"),
                    ["humidity"] = GetInt(entry, "main", "humidity"),
                    ["description"] = weather.description,
                    ["icon"] = weather.icon,
                    ["iconUrl"] = BuildIconUrl(weather.icon),
                    ["windSpeed"] = GetDouble(entry, "wind", "speed"),
                    ["clouds"] = GetInt(entry, "clouds", "all"),
                });
            }
        }

        return new Dictionary<string, object?>
        {
            ["city"] = cityName,
            ["country"] = country,
            ["items"] = items,
        };
    }

    private static (string description, string icon) GetFirstWeather(JsonElement element)
    {
        if (element.TryGetProperty("weather", out var weatherArray) &&
            weatherArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var w in weatherArray.EnumerateArray())
            {
                var description = GetString(w, "description");
                var icon = GetString(w, "icon");
                return (description, icon);
            }
        }

        return (string.Empty, string.Empty);
    }

    private static string BuildIconUrl(string icon)
    {
        return string.IsNullOrWhiteSpace(icon)
            ? string.Empty
            : $"https://openweathermap.org/img/wn/{icon}@2x.png";
    }

    private static string GetString(IReadOnlyDictionary<string, object?> source, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (source.TryGetValue(key, out var value) && value is not null)
            {
                var text = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }

        return string.Empty;
    }

    private static string GetString(JsonElement element, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (element.TryGetProperty(key, out var property) && property.ValueKind == JsonValueKind.String)
            {
                return property.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static string GetNestedString(JsonElement element, string outerKey, string innerKey)
    {
        if (element.TryGetProperty(outerKey, out var outer) && outer.ValueKind == JsonValueKind.Object)
        {
            return GetString(outer, innerKey);
        }

        return string.Empty;
    }

    private static double GetDouble(JsonElement element, string outerKey, string innerKey)
    {
        if (element.TryGetProperty(outerKey, out var outer) &&
            outer.TryGetProperty(innerKey, out var prop))
        {
            if (prop.TryGetDouble(out var value))
            {
                return Math.Round(value, 1);
            }
        }

        return 0.0;
    }

    private static int GetInt(JsonElement element, string outerKey, string innerKey)
    {
        if (element.TryGetProperty(outerKey, out var outer) &&
            outer.TryGetProperty(innerKey, out var prop) &&
            prop.TryGetInt32(out var value))
        {
            return value;
        }

        return 0;
    }

    private static int? GetOptionalInt(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop) && prop.TryGetInt32(out var value))
        {
            return value;
        }

        return null;
    }

    private static long? GetOptionalLong(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop) && prop.TryGetInt64(out var value))
        {
            return value;
        }

        return null;
    }

    private static long? GetOptionalLong(JsonElement element, string outerKey, string innerKey)
    {
        if (element.TryGetProperty(outerKey, out var outer) &&
            outer.TryGetProperty(innerKey, out var prop) &&
            prop.TryGetInt64(out var value))
        {
            return value;
        }

        return null;
    }
}
