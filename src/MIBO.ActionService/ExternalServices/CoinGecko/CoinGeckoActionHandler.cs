using System.Globalization;
using System.Text.Json;
using MIBO.ActionService.ExternalServices.Abstractions;
using MIBO.ActionService.Services;

namespace MIBO.ActionService.ExternalServices.CoinGecko;

public sealed class CoinGeckoActionHandler(ICoinGeckoClient client) : IExternalDataSourceHandler
{
    private static readonly HashSet<string> SupportedHandlers = new(StringComparer.OrdinalIgnoreCase)
    {
        "crypto.markets",
        "crypto.price",
        "crypto.trending",
        "crypto.search",
        "crypto.coin.detail",
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
            "crypto.markets" => GetMarketsAsync(args, cancellationToken),
            "crypto.price" => GetPriceAsync(args, cancellationToken),
            "crypto.trending" => GetTrendingAsync(cancellationToken),
            "crypto.search" => SearchCoinsAsync(args, cancellationToken),
            "crypto.coin.detail" => GetCoinDetailAsync(args, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported CoinGecko handler '{handler}'."),
        };
    }

    public DataFieldHints GetFieldHints(string handler)
    {
        return handler switch
        {
            "crypto.markets" => new DataFieldHints
            {
                EntityType = "crypto_market_list",
                CollectionPath = "items",
                LabelField = "name",
                ValueField = "currentPrice",
                TitleField = "name",
                ImageField = "image",
                SearchField = "name",
            },
            "crypto.price" => new DataFieldHints
            {
                EntityType = "crypto_price",
                CollectionPath = "items",
                LabelField = "id",
                ValueField = "usd",
            },
            "crypto.trending" => new DataFieldHints
            {
                EntityType = "crypto_trending",
                CollectionPath = "items",
                LabelField = "name",
                ValueField = "marketCapRank",
                TitleField = "name",
                ImageField = "thumb",
                SearchField = "name",
            },
            "crypto.search" => new DataFieldHints
            {
                EntityType = "crypto_search",
                CollectionPath = "items",
                LabelField = "name",
                ValueField = "marketCapRank",
                TitleField = "name",
                ImageField = "thumb",
                SearchField = "name",
            },
            "crypto.coin.detail" => new DataFieldHints
            {
                EntityType = "crypto_coin",
                LabelField = "name",
                ValueField = "currentPrice",
                TitleField = "name",
                ImageField = "image",
            },
            _ => new DataFieldHints(),
        };
    }

    private async Task<object> GetMarketsAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var vsCurrency = GetString(args, "vs_currency", "currency");
        if (string.IsNullOrWhiteSpace(vsCurrency))
        {
            vsCurrency = "usd";
        }

        var limit = Math.Max(1, GetInt(args, 20, "limit", "per_page"));
        var page = Math.Max(1, GetInt(args, 1, "page"));

        var path = $"/coins/markets?vs_currency={Uri.EscapeDataString(vsCurrency)}&per_page={limit}&page={page}&order=market_cap_desc";
        var payload = await client.GetAsync(path, cancellationToken);

        var items = new List<Dictionary<string, object?>>();
        if (payload.ValueKind == JsonValueKind.Array)
        {
            foreach (var coin in payload.EnumerateArray())
            {
                items.Add(MapMarketCoin(coin));
            }
        }

        return new
        {
            items,
            total = items.Count,
            vsCurrency,
            page,
            limit,
        };
    }

    private async Task<object> GetPriceAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var ids = GetString(args, "ids", "id", "coins");
        if (string.IsNullOrWhiteSpace(ids))
        {
            throw new InvalidOperationException("crypto.price requires an ids argument (comma-separated coin ids).");
        }

        var vsCurrencies = GetString(args, "vs_currencies", "currency");
        if (string.IsNullOrWhiteSpace(vsCurrencies))
        {
            vsCurrencies = "usd";
        }

        var path = $"/simple/price?ids={Uri.EscapeDataString(ids)}&vs_currencies={Uri.EscapeDataString(vsCurrencies)}";
        var payload = await client.GetAsync(path, cancellationToken);

        var items = new List<Dictionary<string, object?>>();
        if (payload.ValueKind == JsonValueKind.Object)
        {
            foreach (var coin in payload.EnumerateObject())
            {
                var entry = new Dictionary<string, object?>
                {
                    ["id"] = coin.Name,
                };

                if (coin.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var currency in coin.Value.EnumerateObject())
                    {
                        if (currency.Value.TryGetDecimal(out var price))
                        {
                            entry[currency.Name] = price;
                        }
                    }
                }

                items.Add(entry);
            }
        }

        return new
        {
            items,
            ids,
            vsCurrencies,
        };
    }

    private async Task<object> GetTrendingAsync(CancellationToken cancellationToken)
    {
        var payload = await client.GetAsync("/search/trending", cancellationToken);

        var items = new List<Dictionary<string, object?>>();
        if (payload.TryGetProperty("coins", out var coinsArray) && coinsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in coinsArray.EnumerateArray())
            {
                if (entry.TryGetProperty("item", out var item))
                {
                    items.Add(new Dictionary<string, object?>
                    {
                        ["id"] = GetString(item, "id"),
                        ["coinId"] = GetInt(item, "coin_id"),
                        ["name"] = GetString(item, "name"),
                        ["symbol"] = GetString(item, "symbol"),
                        ["marketCapRank"] = GetOptionalInt(item, "market_cap_rank"),
                        ["thumb"] = GetString(item, "thumb"),
                        ["score"] = GetOptionalInt(item, "score"),
                    });
                }
            }
        }

        return new
        {
            items,
            total = items.Count,
        };
    }

    private async Task<object> SearchCoinsAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var query = GetString(args, "q", "query", "search");
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new InvalidOperationException("crypto.search requires a query argument.");
        }

        var path = $"/search?query={Uri.EscapeDataString(query)}";
        var payload = await client.GetAsync(path, cancellationToken);

        var items = new List<Dictionary<string, object?>>();
        if (payload.TryGetProperty("coins", out var coinsArray) && coinsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var coin in coinsArray.EnumerateArray())
            {
                items.Add(new Dictionary<string, object?>
                {
                    ["id"] = GetString(coin, "id"),
                    ["name"] = GetString(coin, "name"),
                    ["symbol"] = GetString(coin, "symbol"),
                    ["marketCapRank"] = GetOptionalInt(coin, "market_cap_rank"),
                    ["thumb"] = GetString(coin, "thumb"),
                    ["apiSymbol"] = GetString(coin, "api_symbol"),
                });
            }
        }

        return new
        {
            items,
            total = items.Count,
            query,
        };
    }

    private async Task<object> GetCoinDetailAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var coinId = GetString(args, "id", "coinId", "coin");
        if (string.IsNullOrWhiteSpace(coinId))
        {
            throw new InvalidOperationException("crypto.coin.detail requires an id argument.");
        }

        var path = $"/coins/{Uri.EscapeDataString(coinId)}?localization=false&tickers=false&community_data=false&developer_data=false";
        var payload = await client.GetAsync(path, cancellationToken);

        return MapCoinDetail(payload);
    }

    private static Dictionary<string, object?> MapMarketCoin(JsonElement coin)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = GetString(coin, "id"),
            ["symbol"] = GetString(coin, "symbol"),
            ["name"] = GetString(coin, "name"),
            ["image"] = GetString(coin, "image"),
            ["currentPrice"] = GetDecimal(coin, "current_price"),
            ["marketCap"] = GetDecimal(coin, "market_cap"),
            ["marketCapRank"] = GetOptionalInt(coin, "market_cap_rank"),
            ["totalVolume"] = GetDecimal(coin, "total_volume"),
            ["priceChangePercentage24h"] = GetDecimal(coin, "price_change_percentage_24h"),
            ["high24h"] = GetDecimal(coin, "high_24h"),
            ["low24h"] = GetDecimal(coin, "low_24h"),
        };
    }

    private static Dictionary<string, object?> MapCoinDetail(JsonElement payload)
    {
        var currentPrice = 0m;
        var marketCap = 0m;
        var totalVolume = 0m;
        var priceChange24h = 0m;

        if (payload.TryGetProperty("market_data", out var marketData))
        {
            currentPrice = GetNestedDecimal(marketData, "current_price", "usd");
            marketCap = GetNestedDecimal(marketData, "market_cap", "usd");
            totalVolume = GetNestedDecimal(marketData, "total_volume", "usd");
            priceChange24h = GetDecimal(marketData, "price_change_percentage_24h");
        }

        var imageUrl = string.Empty;
        if (payload.TryGetProperty("image", out var image))
        {
            imageUrl = GetString(image, "large", "small", "thumb");
        }

        return new Dictionary<string, object?>
        {
            ["id"] = GetString(payload, "id"),
            ["symbol"] = GetString(payload, "symbol"),
            ["name"] = GetString(payload, "name"),
            ["image"] = imageUrl,
            ["currentPrice"] = currentPrice,
            ["marketCap"] = marketCap,
            ["marketCapRank"] = GetOptionalInt(payload, "market_cap_rank"),
            ["totalVolume"] = totalVolume,
            ["priceChangePercentage24h"] = priceChange24h,
            ["description"] = GetNestedString(payload, "description", "en"),
            ["genesisDate"] = GetString(payload, "genesis_date"),
            ["homepageUrl"] = GetFirstArrayString(payload, "links", "homepage"),
        };
    }

    private static string GetFirstArrayString(JsonElement element, string outerKey, string innerKey)
    {
        if (element.TryGetProperty(outerKey, out var outer) &&
            outer.TryGetProperty(innerKey, out var arr) &&
            arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var text = item.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
            }
        }

        return string.Empty;
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

    private static int? GetOptionalInt(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop) && prop.TryGetInt32(out var value))
        {
            return value;
        }

        return null;
    }

    private static int GetInt(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop) && prop.TryGetInt32(out var value))
        {
            return value;
        }

        return 0;
    }

    private static decimal GetDecimal(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var property))
        {
            if (property.TryGetDecimal(out var parsed))
            {
                return parsed;
            }
        }

        return 0m;
    }

    private static decimal GetNestedDecimal(JsonElement element, string outerKey, string innerKey)
    {
        if (element.TryGetProperty(outerKey, out var outer) && outer.ValueKind == JsonValueKind.Object)
        {
            return GetDecimal(outer, innerKey);
        }

        return 0m;
    }
}