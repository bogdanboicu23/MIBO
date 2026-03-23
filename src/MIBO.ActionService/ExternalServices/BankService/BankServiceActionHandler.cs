using System.Globalization;
using System.Text;
using System.Text.Json;
using MIBO.ActionService.ExternalServices.Abstractions;
using MIBO.ActionService.Services;

namespace MIBO.ActionService.ExternalServices.BankService;

public sealed class BankServiceActionHandler(IBankServiceClient bankServiceClient) : IExternalDataSourceHandler
{
    private static readonly HashSet<string> SupportedHandlers = new(StringComparer.OrdinalIgnoreCase)
    {
        "finance.accounts.list",
        "finance.accounts.get",
        "finance.transactions.list",
        "finance.transactions.search",
        "finance.expenses.list",
        "finance.expenses.categories",
        "finance.budgets.list",
        "finance.summary.get",
        "finance.analytics.expenses",
        "finance.analytics.transactions",
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
            "finance.accounts.list" => GetAccountsAsync(args, cancellationToken),
            "finance.accounts.get" => GetAccountByIdAsync(args, cancellationToken),
            "finance.transactions.list" => GetTransactionsAsync(args, cancellationToken),
            "finance.transactions.search" => SearchTransactionsAsync(args, cancellationToken),
            "finance.expenses.list" => GetExpensesAsync(args, cancellationToken),
            "finance.expenses.categories" => GetExpenseCategoriesAsync(cancellationToken),
            "finance.budgets.list" => GetBudgetsAsync(args, cancellationToken),
            "finance.summary.get" => GetSummaryAsync(args, cancellationToken),
            "finance.analytics.expenses" => GetExpenseAnalyticsAsync(args, cancellationToken),
            "finance.analytics.transactions" => GetTransactionAnalyticsAsync(args, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported BankService handler '{handler}'."),
        };
    }

    public DataFieldHints GetFieldHints(string handler)
    {
        return handler switch
        {
            "finance.accounts.list" => new DataFieldHints
            {
                EntityType = "account_list",
                CollectionPath = "items",
                LabelField = "accountName",
                ValueField = "balance",
                TitleField = "accountName",
                CategoryField = "type",
            },
            "finance.accounts.get" => new DataFieldHints
            {
                EntityType = "account",
                LabelField = "accountName",
                ValueField = "balance",
                TitleField = "accountName",
                CategoryField = "type",
            },
            "finance.transactions.list" or "finance.transactions.search" => new DataFieldHints
            {
                EntityType = "transaction_list",
                CollectionPath = "items",
                LabelField = "description",
                ValueField = "amount",
                TitleField = "description",
                CategoryField = "category",
            },
            "finance.expenses.list" => new DataFieldHints
            {
                EntityType = "expense_list",
                CollectionPath = "items",
                LabelField = "description",
                ValueField = "amount",
                TitleField = "description",
                CategoryField = "category",
            },
            "finance.expenses.categories" => new DataFieldHints
            {
                EntityType = "category_list",
                CollectionPath = "items",
                LabelField = "name",
                TitleField = "name",
            },
            "finance.budgets.list" => new DataFieldHints
            {
                EntityType = "budget_list",
                CollectionPath = "items",
                LabelField = "name",
                ValueField = "limit",
                TitleField = "name",
                CategoryField = "category",
            },
            "finance.summary.get" => new DataFieldHints
            {
                EntityType = "financial_summary",
                LabelField = "userId",
                ValueField = "totalBalance",
            },
            "finance.analytics.expenses" => new DataFieldHints
            {
                EntityType = "expense_analytics",
                CollectionPath = "byCategory",
                LabelField = "category",
                ValueField = "amount",
                CategoryField = "category",
            },
            "finance.analytics.transactions" => new DataFieldHints
            {
                EntityType = "transaction_analytics",
                LabelField = "userId",
                ValueField = "netFlow",
            },
            _ => new DataFieldHints(),
        };
    }

    private async Task<object> GetAccountsAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var userId = GetString(args, "userId", "user_id");
        var limit = Math.Max(1, GetInt(args, 30, "limit"));
        var skip = Math.Max(0, GetInt(args, 0, "skip"));
        var sortBy = GetString(args, "sortBy");
        var order = GetString(args, "order");

        var query = new Dictionary<string, string?> { ["skip"] = skip.ToString(CultureInfo.InvariantCulture), ["limit"] = limit.ToString(CultureInfo.InvariantCulture) };
        if (!string.IsNullOrWhiteSpace(userId)) query["userId"] = userId;
        if (!string.IsNullOrWhiteSpace(sortBy)) query["sortBy"] = sortBy;
        if (!string.IsNullOrWhiteSpace(order)) query["order"] = order;

        return await bankServiceClient.GetAsync(BuildPathWithQuery("/api/accounts", query), cancellationToken);
    }

    private async Task<object> GetAccountByIdAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var id = GetInt(args, 0, "id", "accountId");
        if (id <= 0) throw new InvalidOperationException("finance.accounts.get requires id.");

        return await bankServiceClient.GetAsync($"/api/accounts/{id}", cancellationToken);
    }

    private async Task<object> GetTransactionsAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var userId = GetString(args, "userId", "user_id");
        var limit = Math.Max(1, GetInt(args, 30, "limit"));
        var skip = Math.Max(0, GetInt(args, 0, "skip"));
        var type = GetString(args, "type");
        var category = GetString(args, "category");
        var sortBy = GetString(args, "sortBy");
        var order = GetString(args, "order");

        var query = new Dictionary<string, string?> { ["skip"] = skip.ToString(CultureInfo.InvariantCulture), ["limit"] = limit.ToString(CultureInfo.InvariantCulture) };
        if (!string.IsNullOrWhiteSpace(userId)) query["userId"] = userId;
        if (!string.IsNullOrWhiteSpace(type)) query["type"] = type;
        if (!string.IsNullOrWhiteSpace(category)) query["category"] = category;
        if (!string.IsNullOrWhiteSpace(sortBy)) query["sortBy"] = sortBy;
        if (!string.IsNullOrWhiteSpace(order)) query["order"] = order;

        return await bankServiceClient.GetAsync(BuildPathWithQuery("/api/transactions", query), cancellationToken);
    }

    private async Task<object> SearchTransactionsAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var q = GetString(args, "q", "query");
        var limit = Math.Max(1, GetInt(args, 30, "limit"));
        var skip = Math.Max(0, GetInt(args, 0, "skip"));

        if (string.IsNullOrWhiteSpace(q))
            throw new InvalidOperationException("finance.transactions.search requires q.");

        var query = new Dictionary<string, string?>
        {
            ["q"] = q,
            ["skip"] = skip.ToString(CultureInfo.InvariantCulture),
            ["limit"] = limit.ToString(CultureInfo.InvariantCulture),
        };

        return await bankServiceClient.GetAsync(BuildPathWithQuery("/api/transactions/search", query), cancellationToken);
    }

    private async Task<object> GetExpensesAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var userId = GetString(args, "userId", "user_id");
        var limit = Math.Max(1, GetInt(args, 30, "limit"));
        var skip = Math.Max(0, GetInt(args, 0, "skip"));
        var category = GetString(args, "category");
        var sortBy = GetString(args, "sortBy");
        var order = GetString(args, "order");

        var query = new Dictionary<string, string?> { ["skip"] = skip.ToString(CultureInfo.InvariantCulture), ["limit"] = limit.ToString(CultureInfo.InvariantCulture) };
        if (!string.IsNullOrWhiteSpace(userId)) query["userId"] = userId;
        if (!string.IsNullOrWhiteSpace(category)) query["category"] = category;
        if (!string.IsNullOrWhiteSpace(sortBy)) query["sortBy"] = sortBy;
        if (!string.IsNullOrWhiteSpace(order)) query["order"] = order;

        return await bankServiceClient.GetAsync(BuildPathWithQuery("/api/expenses", query), cancellationToken);
    }

    private async Task<object> GetExpenseCategoriesAsync(CancellationToken cancellationToken)
    {
        var payload = await bankServiceClient.GetAsync("/api/expenses/categories", cancellationToken);
        var categories = new List<object>();

        if (payload.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in payload.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.String)
                {
                    var name = entry.GetString() ?? string.Empty;
                    categories.Add(new { name, slug = name });
                }
            }
        }

        return new { items = categories };
    }

    private async Task<object> GetBudgetsAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var userId = GetString(args, "userId", "user_id");
        var limit = Math.Max(1, GetInt(args, 30, "limit"));
        var skip = Math.Max(0, GetInt(args, 0, "skip"));
        var category = GetString(args, "category");
        var period = GetString(args, "period");

        var query = new Dictionary<string, string?> { ["skip"] = skip.ToString(CultureInfo.InvariantCulture), ["limit"] = limit.ToString(CultureInfo.InvariantCulture) };
        if (!string.IsNullOrWhiteSpace(userId)) query["userId"] = userId;
        if (!string.IsNullOrWhiteSpace(category)) query["category"] = category;
        if (!string.IsNullOrWhiteSpace(period)) query["period"] = period;

        return await bankServiceClient.GetAsync(BuildPathWithQuery("/api/budgets", query), cancellationToken);
    }

    private async Task<object> GetSummaryAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var userId = GetString(args, "userId", "user_id");
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("finance.summary.get requires userId.");

        return await bankServiceClient.GetAsync($"/api/summary/user/{Uri.EscapeDataString(userId)}", cancellationToken);
    }

    private async Task<object> GetExpenseAnalyticsAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var userId = GetString(args, "userId", "user_id");
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("finance.analytics.expenses requires userId.");

        var period = GetString(args, "period");
        var query = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(period)) query["period"] = period;

        return await bankServiceClient.GetAsync(
            BuildPathWithQuery($"/api/analytics/expenses/user/{Uri.EscapeDataString(userId)}", query),
            cancellationToken);
    }

    private async Task<object> GetTransactionAnalyticsAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var userId = GetString(args, "userId", "user_id");
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("finance.analytics.transactions requires userId.");

        var period = GetString(args, "period");
        var query = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(period)) query["period"] = period;

        return await bankServiceClient.GetAsync(
            BuildPathWithQuery($"/api/analytics/transactions/user/{Uri.EscapeDataString(userId)}", query),
            cancellationToken);
    }

    private static string BuildPathWithQuery(string path, IReadOnlyDictionary<string, string?> query)
    {
        var builder = new StringBuilder(path);
        var first = true;
        foreach (var (key, value) in query)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            builder.Append(first ? '?' : '&');
            builder.Append(Uri.EscapeDataString(key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(value));
            first = false;
        }

        return builder.ToString();
    }

    private static int GetInt(IReadOnlyDictionary<string, object?> source, int fallback, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (source.TryGetValue(key, out var value) && value is not null)
            {
                if (int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed))
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
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
        }

        return string.Empty;
    }
}
