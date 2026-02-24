using MIBO.FinanceDataService.Dtos;
using MIBO.FinanceDataService.Interfaces;
using MIBO.FinanceDataService.Models;
using System.Linq.Dynamic.Core;

namespace MIBO.FinanceDataService.Services;

public class FinancialService : IFinancialService
{
    private readonly MockDataService _mockDataService;

    public FinancialService(MockDataService mockDataService)
    {
        _mockDataService = mockDataService;
    }

    // Accounts
    public AccountsResponse GetAccounts(int? userId = null, int skip = 0, int limit = 30, string? select = null, string? sortBy = null, string? order = null)
    {
        var query = _mockDataService.GetAccounts().AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId);

        var total = query.Count();

        // Apply sorting
        if (!string.IsNullOrEmpty(sortBy))
        {
            var orderDirection = order?.ToLower() == "desc" ? "descending" : "ascending";
            query = query.OrderBy($"{sortBy} {orderDirection}");
        }
        else
        {
            query = query.OrderBy(a => a.Id);
        }

        // Apply pagination
        var accounts = query.Skip(skip).Take(limit).ToArray();

        return new AccountsResponse(accounts, total, skip, limit);
    }

    public Account? GetAccountById(int id) => _mockDataService.GetAccountById(id);

    public Account? GetAccountByNumber(string accountNumber) =>
        _mockDataService.GetAccounts().FirstOrDefault(a => a.AccountNumber == accountNumber);

    // Transactions
    public TransactionsResponse GetTransactions(int? userId = null, int? accountId = null, int skip = 0, int limit = 30,
        string? type = null, string? category = null, DateTime? startDate = null, DateTime? endDate = null,
        string? sortBy = null, string? order = null)
    {
        var query = _mockDataService.GetTransactions().AsQueryable();

        if (userId.HasValue)
            query = query.Where(t => t.UserId == userId);

        if (accountId.HasValue)
            query = query.Where(t => t.AccountId == accountId);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(t => t.Type == type);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category == category);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate);

        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate);

        var total = query.Count();

        // Apply sorting
        if (!string.IsNullOrEmpty(sortBy))
        {
            var orderDirection = order?.ToLower() == "desc" ? "descending" : "ascending";
            query = query.OrderBy($"{sortBy} {orderDirection}");
        }
        else
        {
            query = query.OrderByDescending(t => t.Date);
        }

        // Apply pagination
        var transactions = query.Skip(skip).Take(limit).ToArray();

        return new TransactionsResponse(transactions, total, skip, limit);
    }

    public Transaction? GetTransactionById(int id) => _mockDataService.GetTransactionById(id);

    public TransactionsResponse SearchTransactions(string q, int skip = 0, int limit = 30)
    {
        var query = _mockDataService.GetTransactions()
            .Where(t => t.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                       t.Merchant.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                       t.Category.Contains(q, StringComparison.OrdinalIgnoreCase))
            .AsQueryable();

        var total = query.Count();
        var transactions = query.Skip(skip).Take(limit).ToArray();

        return new TransactionsResponse(transactions, total, skip, limit);
    }

    // Expenses
    public ExpensesResponse GetExpenses(int? userId = null, int skip = 0, int limit = 30,
        string? category = null, DateTime? startDate = null, DateTime? endDate = null,
        string? sortBy = null, string? order = null)
    {
        var query = _mockDataService.GetExpenses().AsQueryable();

        if (userId.HasValue)
            query = query.Where(e => e.UserId == userId);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(e => e.Category == category);

        if (startDate.HasValue)
            query = query.Where(e => e.Date >= startDate);

        if (endDate.HasValue)
            query = query.Where(e => e.Date <= endDate);

        var total = query.Count();

        // Apply sorting
        if (!string.IsNullOrEmpty(sortBy))
        {
            var orderDirection = order?.ToLower() == "desc" ? "descending" : "ascending";
            query = query.OrderBy($"{sortBy} {orderDirection}");
        }
        else
        {
            query = query.OrderByDescending(e => e.Date);
        }

        // Apply pagination
        var expenses = query.Skip(skip).Take(limit).ToArray();

        return new ExpensesResponse(expenses, total, skip, limit);
    }

    public Expense? GetExpenseById(int id) => _mockDataService.GetExpenseById(id);

    // Budgets
    public BudgetsResponse GetBudgets(int? userId = null, int skip = 0, int limit = 30,
        string? category = null, bool? isActive = null, string? period = null)
    {
        var query = _mockDataService.GetBudgets().AsQueryable();

        if (userId.HasValue)
            query = query.Where(b => b.UserId == userId);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(b => b.Category == category);

        if (isActive.HasValue)
            query = query.Where(b => b.IsActive == isActive);

        if (!string.IsNullOrEmpty(period))
            query = query.Where(b => b.Period == period);

        var total = query.Count();
        var budgets = query.Skip(skip).Take(limit).ToArray();

        return new BudgetsResponse(budgets, total, skip, limit);
    }

    public Budget? GetBudgetById(int id) => _mockDataService.GetBudgetById(id);

    // Summary
    public FinancialSummaryDto GetFinancialSummary(int userId)
    {
        var accounts = _mockDataService.GetAccountsByUserId(userId);
        var transactions = _mockDataService.GetTransactionsByUserId(userId);
        var expenses = _mockDataService.GetExpensesByUserId(userId);

        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);

        var thisMonthExpenses = expenses.Where(e => e.Date >= startOfMonth).Sum(e => e.Amount);
        var lastMonthExpenses = expenses.Where(e => e.Date >= startOfLastMonth && e.Date < startOfMonth).Sum(e => e.Amount);

        var expensesByCategory = expenses
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var accountBalances = accounts
            .ToDictionary(a => a.AccountName, a => a.Balance);

        return new FinancialSummaryDto
        {
            UserId = userId,
            TotalBalance = accounts.Sum(a => a.Balance),
            TotalIncome = transactions.Where(t => t.Type == "credit").Sum(t => t.Amount),
            TotalExpenses = transactions.Where(t => t.Type == "debit").Sum(t => t.Amount),
            AccountsCount = accounts.Count,
            PrimaryCurrency = "USD",
            LastTransactionDate = transactions.OrderByDescending(t => t.Date).FirstOrDefault()?.Date ?? DateTime.Now,
            ExpenseSummary = new ExpenseSummary
            {
                ByCategory = expensesByCategory,
                ThisMonth = thisMonthExpenses,
                LastMonth = lastMonthExpenses,
                Average = expenses.Count > 0 ? expenses.Average(e => e.Amount) : 0
            },
            AccountBalances = accountBalances
        };
    }

    // Analytics
    public ExpenseAnalyticsDto GetExpenseAnalytics(int userId, DateTime? startDate = null, DateTime? endDate = null, string? period = null)
    {
        // Handle period-based date ranges
        if (!startDate.HasValue && !endDate.HasValue && !string.IsNullOrEmpty(period))
        {
            var now = DateTime.Now;
            switch (period?.ToLower())
            {
                case "today":
                    startDate = now.Date;
                    endDate = now.Date.AddDays(1).AddSeconds(-1);
                    break;
                case "yesterday":
                    startDate = now.Date.AddDays(-1);
                    endDate = now.Date.AddSeconds(-1);
                    break;
                case "thisweek":
                    startDate = now.AddDays(-(int)now.DayOfWeek);
                    endDate = now;
                    break;
                case "lastweek":
                    startDate = now.AddDays(-(int)now.DayOfWeek - 7);
                    endDate = now.AddDays(-(int)now.DayOfWeek).AddSeconds(-1);
                    break;
                case "thismonth":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    endDate = now;
                    break;
                case "lastmonth":
                    startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    endDate = new DateTime(now.Year, now.Month, 1).AddSeconds(-1);
                    break;
                case "last30days":
                    startDate = now.Date.AddDays(-30);
                    endDate = now;
                    break;
                case "last90days":
                    startDate = now.Date.AddDays(-90);
                    endDate = now;
                    break;
                case "thisyear":
                    startDate = new DateTime(now.Year, 1, 1);
                    endDate = now;
                    break;
                case "lastyear":
                    startDate = new DateTime(now.Year - 1, 1, 1);
                    endDate = new DateTime(now.Year, 1, 1).AddSeconds(-1);
                    break;
            }
        }

        startDate ??= DateTime.Now.AddMonths(-1);
        endDate ??= DateTime.Now;

        var expenses = _mockDataService.GetExpensesByUserId(userId)
            .Where(e => e.Date >= startDate && e.Date <= endDate)
            .ToList();

        var totalAmount = expenses.Sum(e => e.Amount);

        // Category breakdown with colors for charts
        var categoryColors = new Dictionary<string, string>
        {
            ["groceries"] = "#4CAF50",
            ["dining"] = "#FF9800",
            ["transport"] = "#2196F3",
            ["shopping"] = "#9C27B0",
            ["entertainment"] = "#FF5722",
            ["utilities"] = "#795548",
            ["rent"] = "#607D8B",
            ["healthcare"] = "#F44336",
            ["education"] = "#3F51B5",
            ["subscriptions"] = "#009688",
            ["other"] = "#9E9E9E"
        };

        var byCategory = expenses
            .GroupBy(e => e.Category)
            .Select(g => new CategoryBreakdown
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount),
                Percentage = totalAmount > 0 ? Math.Round(g.Sum(e => e.Amount) / totalAmount * 100, 2) : 0,
                Count = g.Count(),
                Color = categoryColors.GetValueOrDefault(g.Key.ToLower(), "#9E9E9E")
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        // Time series data
        var timeSeries = expenses
            .GroupBy(e => e.Date.Date)
            .Select(g => new TimeSeriesData
            {
                Date = g.Key,
                Amount = g.Sum(e => e.Amount),
                Count = g.Count()
            })
            .OrderBy(t => t.Date)
            .ToList();

        // Statistics
        var amounts = expenses.Select(e => e.Amount).ToList();
        var statistics = new ExpenseStatistics
        {
            Average = amounts.Any() ? amounts.Average() : 0,
            Median = amounts.Any() ? GetMedian(amounts) : 0,
            Min = amounts.Any() ? amounts.Min() : 0,
            Max = amounts.Any() ? amounts.Max() : 0,
            MostExpensiveCategory = byCategory.FirstOrDefault()?.Category ?? "",
            MostFrequentCategory = expenses.GroupBy(e => e.Category)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? "",
            TotalTransactions = expenses.Count
        };

        return new ExpenseAnalyticsDto
        {
            UserId = userId,
            StartDate = startDate.Value,
            EndDate = endDate.Value,
            TotalAmount = totalAmount,
            Currency = "USD",
            ByCategory = byCategory,
            TimeSeries = timeSeries,
            Statistics = statistics
        };
    }

    public TransactionAnalyticsDto GetTransactionAnalytics(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.Now.AddMonths(-1);
        endDate ??= DateTime.Now;

        var transactions = _mockDataService.GetTransactionsByUserId(userId)
            .Where(t => t.Date >= startDate && t.Date <= endDate)
            .ToList();

        var totalIncome = transactions.Where(t => t.Type == "credit").Sum(t => t.Amount);
        var totalExpenses = transactions.Where(t => t.Type == "debit").Sum(t => t.Amount);

        var byType = transactions
            .GroupBy(t => t.Type)
            .Select(g => new TypeBreakdown
            {
                Type = g.Key,
                Amount = g.Sum(t => t.Amount),
                Count = g.Count(),
                Percentage = Math.Round(g.Sum(t => t.Amount) / (totalIncome + totalExpenses) * 100, 2)
            })
            .ToList();

        // Calculate daily balance
        var dailyBalance = transactions
            .GroupBy(t => t.Date.Date)
            .Select(g => new TimeSeriesData
            {
                Date = g.Key,
                Amount = g.Where(t => t.Type == "credit").Sum(t => t.Amount) -
                        g.Where(t => t.Type == "debit").Sum(t => t.Amount),
                Count = g.Count()
            })
            .OrderBy(t => t.Date)
            .ToList();

        return new TransactionAnalyticsDto
        {
            UserId = userId,
            StartDate = startDate.Value,
            EndDate = endDate.Value,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetFlow = totalIncome - totalExpenses,
            Currency = "USD",
            ByType = byType,
            DailyBalance = dailyBalance
        };
    }

    public List<CategoryBreakdown> GetExpensesByCategory(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var analytics = GetExpenseAnalytics(userId, startDate, endDate);
        return analytics.ByCategory;
    }

    private decimal GetMedian(List<decimal> values)
    {
        if (!values.Any()) return 0;

        var sorted = values.OrderBy(v => v).ToList();
        var count = sorted.Count;

        if (count % 2 == 0)
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
        else
            return sorted[count / 2];
    }
}