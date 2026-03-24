using System.Globalization;
using System.Text.Json;
using MIBO.ActionService.ExternalServices.Abstractions;
using MIBO.ActionService.Services;

namespace MIBO.ActionService.ExternalServices.NewsApi;

public sealed class NewsApiActionHandler(INewsApiClient client) : IExternalDataSourceHandler
{
    private static readonly HashSet<string> SupportedHandlers = new(StringComparer.OrdinalIgnoreCase)
    {
        "news.headlines",
        "news.search",
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
            "news.headlines" => GetHeadlinesAsync(args, cancellationToken),
            "news.search" => SearchArticlesAsync(args, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported NewsAPI handler '{handler}'."),
        };
    }

    public DataFieldHints GetFieldHints(string handler)
    {
        return handler switch
        {
            "news.headlines" => new DataFieldHints
            {
                EntityType = "news_headlines",
                CollectionPath = "items",
                LabelField = "title",
                ValueField = "source",
                TitleField = "title",
                ImageField = "urlToImage",
                SearchField = "title",
            },
            "news.search" => new DataFieldHints
            {
                EntityType = "news_search",
                CollectionPath = "items",
                LabelField = "title",
                ValueField = "source",
                TitleField = "title",
                ImageField = "urlToImage",
                SearchField = "title",
            },
            _ => new DataFieldHints(),
        };
    }

    private async Task<object> GetHeadlinesAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var country = GetString(args, "country");
        if (string.IsNullOrWhiteSpace(country))
        {
            country = "us";
        }

        var category = GetString(args, "category");
        var limit = Math.Max(1, GetInt(args, 20, "limit", "pageSize"));

        var path = $"/top-headlines?country={Uri.EscapeDataString(country)}&pageSize={limit}";
        if (!string.IsNullOrWhiteSpace(category))
        {
            path += $"&category={Uri.EscapeDataString(category)}";
        }

        var payload = await client.GetAsync(path, cancellationToken);
        var items = ExtractArticles(payload);

        return new
        {
            items,
            total = GetInt(payload, "totalResults"),
            country,
            category,
        };
    }

    private async Task<object> SearchArticlesAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var query = GetString(args, "q", "query", "search");
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new InvalidOperationException("news.search requires a query argument.");
        }

        var limit = Math.Max(1, GetInt(args, 20, "limit", "pageSize"));
        var sortBy = GetString(args, "sortBy");
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            sortBy = "publishedAt";
        }

        var path = $"/everything?q={Uri.EscapeDataString(query)}&pageSize={limit}&sortBy={Uri.EscapeDataString(sortBy)}";
        var payload = await client.GetAsync(path, cancellationToken);
        var items = ExtractArticles(payload);

        return new
        {
            items,
            total = GetInt(payload, "totalResults"),
            query,
            sortBy,
        };
    }

    private static List<Dictionary<string, object?>> ExtractArticles(JsonElement payload)
    {
        var results = new List<Dictionary<string, object?>>();
        if (!payload.TryGetProperty("articles", out var articlesElement) ||
            articlesElement.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var article in articlesElement.EnumerateArray())
        {
            results.Add(MapArticle(article));
        }

        return results;
    }

    private static Dictionary<string, object?> MapArticle(JsonElement article)
    {
        var sourceName = string.Empty;
        if (article.TryGetProperty("source", out var source) && source.ValueKind == JsonValueKind.Object)
        {
            sourceName = GetString(source, "name");
        }

        return new Dictionary<string, object?>
        {
            ["title"] = GetString(article, "title"),
            ["description"] = GetString(article, "description"),
            ["url"] = GetString(article, "url"),
            ["urlToImage"] = GetString(article, "urlToImage"),
            ["publishedAt"] = GetString(article, "publishedAt"),
            ["source"] = sourceName,
            ["author"] = GetString(article, "author"),
            ["content"] = GetString(article, "content"),
        };
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

    private static int GetInt(IReadOnlyDictionary<string, object?> source, int fallback, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (source.TryGetValue(key, out var value) && value is not null)
            {
                if (int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed))
                {
                    return parsed;
                }
            }
        }

        return fallback;
    }

    private static int GetInt(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop) && prop.TryGetInt32(out var value))
        {
            return value;
        }

        return 0;
    }
}