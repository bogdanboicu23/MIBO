using System.Globalization;
using System.Text;
using System.Text.Json;
using MIBO.ActionService.ExternalServices.Abstractions;
using MIBO.ActionService.Services;

namespace MIBO.ActionService.ExternalServices.DummyJson;

public sealed class DummyJsonActionHandler(IDummyJsonClient dummyJsonClient) : IExternalDataSourceHandler
{
    private static readonly HashSet<string> SupportedHandlers = new(StringComparer.OrdinalIgnoreCase)
    {
        "products.catalog.query",
        "products.categories.list",
        "products.detail.get",
    };

    private static readonly HashSet<string> ProductQueryStopWords = new(StringComparer.Ordinal)
    {
        "a",
        "an",
        "all",
        "below",
        "chart",
        "compare",
        "comparison",
        "cost",
        "costs",
        "data",
        "find",
        "for",
        "graph",
        "live",
        "list",
        "me",
        "of",
        "please",
        "price",
        "prices",
        "product",
        "products",
        "results",
        "search",
        "show",
        "the",
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
            "products.catalog.query" => QueryProductCatalogAsync(args, cancellationToken),
            "products.categories.list" => GetCategoriesAsync(cancellationToken),
            "products.detail.get" => GetProductAsync(args, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported DummyJSON handler '{handler}'."),
        };
    }

    public DataFieldHints GetFieldHints(string handler)
    {
        return handler switch
        {
            "products.catalog.query" => new DataFieldHints
            {
                EntityType = "product_list",
                CollectionPath = "items",
                LabelField = "title",
                ValueField = "price",
                TitleField = "title",
                ImageField = "thumbnail",
                CategoryField = "category",
                SearchField = "title",
            },
            "products.categories.list" => new DataFieldHints
            {
                EntityType = "category_list",
                CollectionPath = "items",
                LabelField = "name",
                TitleField = "name",
                CategoryField = "slug",
                SearchField = "name",
            },
            "products.detail.get" => new DataFieldHints
            {
                EntityType = "product",
                LabelField = "title",
                ValueField = "price",
                TitleField = "title",
                ImageField = "thumbnail",
                CategoryField = "category",
                SearchField = "title",
            },
            _ => new DataFieldHints(),
        };
    }

    private async Task<object> QueryProductCatalogAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var query = GetString(args, "q", "query");
        var category = GetString(args, "category");
        var sort = GetString(args, "sort");
        var sortBy = GetString(args, "sortBy");
        var order = GetString(args, "order");
        var limit = Math.Max(1, GetInt(args, 10, "limit"));
        var skip = Math.Max(0, GetInt(args, 0, "skip"));

        var path = !string.IsNullOrWhiteSpace(query)
            ? "/products/search"
            : !string.IsNullOrWhiteSpace(category)
                ? $"/products/category/{Uri.EscapeDataString(category)}"
                : "/products";

        var queryString = new Dictionary<string, string?>
        {
            ["limit"] = limit.ToString(CultureInfo.InvariantCulture),
            ["skip"] = skip.ToString(CultureInfo.InvariantCulture),
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            queryString["q"] = query;
        }

        var payload = await dummyJsonClient.GetAsync(BuildPathWithQuery(path, queryString), cancellationToken);
        var products = ExtractProducts(payload);
        string? matchedQuery = null;

        if (!string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(category) && products.Count == 0)
        {
            foreach (var fallbackQuery in BuildFallbackProductQueries(query))
            {
                var fallbackQueryString = new Dictionary<string, string?>
                {
                    ["q"] = fallbackQuery,
                    ["limit"] = limit.ToString(CultureInfo.InvariantCulture),
                    ["skip"] = skip.ToString(CultureInfo.InvariantCulture),
                };

                var fallbackPayload = await dummyJsonClient.GetAsync(
                    BuildPathWithQuery("/products/search", fallbackQueryString),
                    cancellationToken);
                var fallbackProducts = ExtractProducts(fallbackPayload);
                if (fallbackProducts.Count == 0)
                {
                    continue;
                }

                payload = fallbackPayload;
                products = fallbackProducts;
                matchedQuery = fallbackQuery;
                break;
            }
        }

        var sortedProducts = SortProducts(products, sort, sortBy, order);

        return new
        {
            items = sortedProducts,
            products = sortedProducts,
            total = GetInt(payload, sortedProducts.Count, "total"),
            limit = GetInt(payload, limit, "limit"),
            skip = GetInt(payload, skip, "skip"),
            query,
            matchedQuery,
            category,
            sort = !string.IsNullOrWhiteSpace(sort)
                ? sort
                : !string.IsNullOrWhiteSpace(sortBy)
                    ? $"{sortBy}:{(string.IsNullOrWhiteSpace(order) ? "asc" : order)}"
                    : string.Empty,
            sortBy,
            order = string.IsNullOrWhiteSpace(order) ? "asc" : order,
        };
    }

    private async Task<object> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var payload = await dummyJsonClient.GetAsync("/products/categories", cancellationToken);
        var categories = new List<object>();
        var slugs = new List<string>();

        if (payload.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in payload.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var slug = entry.GetString() ?? string.Empty;
                slugs.Add(slug);
                categories.Add(new
                {
                    name = HumanizeSlug(slug),
                    slug,
                });
            }
        }

        return new
        {
            categories = slugs.ToArray(),
            items = categories,
        };
    }

    private async Task<object> GetProductAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var productId = GetInt(args, 0, "productId", "id");
        if (productId <= 0)
        {
            throw new InvalidOperationException("products.detail.get requires productId.");
        }

        var payload = await dummyJsonClient.GetAsync($"/products/{productId}", cancellationToken);
        return MapProduct(payload);
    }

    private static string BuildPathWithQuery(string path, IReadOnlyDictionary<string, string?> query)
    {
        var builder = new StringBuilder(path);
        var first = true;
        foreach (var (key, value) in query)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            builder.Append(first ? '?' : '&');
            builder.Append(Uri.EscapeDataString(key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(value));
            first = false;
        }

        return builder.ToString();
    }

    private static List<Dictionary<string, object?>> ExtractProducts(JsonElement payload)
    {
        var results = new List<Dictionary<string, object?>>();
        if (!payload.TryGetProperty("products", out var productsElement) || productsElement.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var product in productsElement.EnumerateArray())
        {
            results.Add(MapProduct(product));
        }

        return results;
    }

    private static Dictionary<string, object?> MapProduct(JsonElement product)
    {
        var images = new List<string>();
        if (product.TryGetProperty("images", out var imagesElement) && imagesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var image in imagesElement.EnumerateArray())
            {
                if (image.ValueKind == JsonValueKind.String)
                {
                    images.Add(image.GetString() ?? string.Empty);
                }
            }
        }

        return new Dictionary<string, object?>
        {
            ["id"] = GetInt(product, 0, "id"),
            ["title"] = GetString(product, "title"),
            ["description"] = GetString(product, "description"),
            ["price"] = GetDecimal(product, 0m, "price"),
            ["discountPercentage"] = GetDecimal(product, 0m, "discountPercentage"),
            ["rating"] = GetDecimal(product, 0m, "rating"),
            ["stock"] = GetInt(product, 0, "stock"),
            ["brand"] = GetString(product, "brand"),
            ["category"] = GetString(product, "category"),
            ["thumbnail"] = GetString(product, "thumbnail"),
            ["images"] = images,
        };
    }

    private static List<Dictionary<string, object?>> SortProducts(
        List<Dictionary<string, object?>> products,
        string sort,
        string sortBy,
        string order)
    {
        var resolvedSortBy = sortBy;
        var resolvedOrder = string.IsNullOrWhiteSpace(order) ? "asc" : order.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(resolvedSortBy) && !string.IsNullOrWhiteSpace(sort))
        {
            var parts = sort.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                resolvedSortBy = parts[0];
            }

            if (parts.Length > 1)
            {
                resolvedOrder = parts[1].ToLowerInvariant();
            }
        }

        if (string.IsNullOrWhiteSpace(resolvedSortBy))
        {
            return products;
        }

        return resolvedOrder == "desc"
            ? products.OrderByDescending(product => ComparableValue(product, resolvedSortBy)).ToList()
            : products.OrderBy(product => ComparableValue(product, resolvedSortBy)).ToList();
    }

    private static IEnumerable<string> BuildFallbackProductQueries(string rawQuery)
    {
        var cleanedTokens = rawQuery
            .Split([' ', '\t', '\r', '\n', ',', '.', ';', ':', '/', '\\', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim().ToLowerInvariant())
            .Where(token => token.Length >= 3)
            .Where(token => !ProductQueryStopWords.Contains(token))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (cleanedTokens.Count == 0)
        {
            yield break;
        }

        var joined = string.Join(' ', cleanedTokens);
        if (!string.Equals(joined, rawQuery.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            yield return joined;
        }

        foreach (var token in cleanedTokens)
        {
            yield return token;

            if (token.EndsWith("s", StringComparison.Ordinal) && token.Length > 3)
            {
                yield return token[..^1];
            }
            else if (!token.EndsWith("s", StringComparison.Ordinal))
            {
                yield return $"{token}s";
            }
        }
    }

    private static object? ComparableValue(IReadOnlyDictionary<string, object?> product, string key)
    {
        if (!product.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            decimal number => number,
            double number => number,
            float number => number,
            int number => number,
            long number => number,
            _ => value?.ToString(),
        };
    }

    private static string HumanizeSlug(string slug)
    {
        return string.Join(" ", slug.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
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

    private static int GetInt(JsonElement element, int fallback, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (element.TryGetProperty(key, out var property) && property.TryGetInt32(out var parsed))
            {
                return parsed;
            }
        }

        return fallback;
    }

    private static decimal GetDecimal(JsonElement element, decimal fallback, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (element.TryGetProperty(key, out var property) && property.TryGetDecimal(out var parsed))
            {
                return parsed;
            }
        }

        return fallback;
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
}
