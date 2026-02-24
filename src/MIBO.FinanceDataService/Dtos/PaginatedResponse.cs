namespace MIBO.FinanceDataService.Dtos;

public class PaginatedResponse<T>
{
    public T[] Items { get; set; } = Array.Empty<T>();
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }

    public PaginatedResponse() { }

    public PaginatedResponse(T[] items, int total, int skip, int limit)
    {
        Items = items;
        Total = total;
        Skip = skip;
        Limit = limit;
    }
}

public class AccountsResponse : PaginatedResponse<Models.Account>
{
    public AccountsResponse() { }
    public AccountsResponse(Models.Account[] accounts, int total, int skip, int limit)
        : base(accounts, total, skip, limit) { }
}

public class TransactionsResponse : PaginatedResponse<Models.Transaction>
{
    public TransactionsResponse() { }
    public TransactionsResponse(Models.Transaction[] transactions, int total, int skip, int limit)
        : base(transactions, total, skip, limit) { }
}

public class ExpensesResponse : PaginatedResponse<Models.Expense>
{
    public ExpensesResponse() { }
    public ExpensesResponse(Models.Expense[] expenses, int total, int skip, int limit)
        : base(expenses, total, skip, limit) { }
}

public class BudgetsResponse : PaginatedResponse<Models.Budget>
{
    public BudgetsResponse() { }
    public BudgetsResponse(Models.Budget[] budgets, int total, int skip, int limit)
        : base(budgets, total, skip, limit) { }
}