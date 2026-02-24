using MIBO.FinanceDataService.Dtos;
using MIBO.FinanceDataService.Models;

namespace MIBO.FinanceDataService.Interfaces;

public interface IFinancialService
{
    // Accounts
    AccountsResponse GetAccounts(int? userId = null, int skip = 0, int limit = 30, string? select = null, string? sortBy = null, string? order = null);
    Account? GetAccountById(int id);
    Account? GetAccountByNumber(string accountNumber);

    // Transactions
    TransactionsResponse GetTransactions(int? userId = null, int? accountId = null, int skip = 0, int limit = 30,
        string? type = null, string? category = null, DateTime? startDate = null, DateTime? endDate = null,
        string? sortBy = null, string? order = null);
    Transaction? GetTransactionById(int id);
    TransactionsResponse SearchTransactions(string q, int skip = 0, int limit = 30);

    // Expenses
    ExpensesResponse GetExpenses(int? userId = null, int skip = 0, int limit = 30,
        string? category = null, DateTime? startDate = null, DateTime? endDate = null,
        string? sortBy = null, string? order = null);
    Expense? GetExpenseById(int id);

    // Budgets
    BudgetsResponse GetBudgets(int? userId = null, int skip = 0, int limit = 30,
        string? category = null, bool? isActive = null, string? period = null);
    Budget? GetBudgetById(int id);

    // Summary
    FinancialSummaryDto GetFinancialSummary(int userId);

    // Analytics
    ExpenseAnalyticsDto GetExpenseAnalytics(int userId, DateTime? startDate = null, DateTime? endDate = null, string? period = null);
    TransactionAnalyticsDto GetTransactionAnalytics(int userId, DateTime? startDate = null, DateTime? endDate = null);
    List<CategoryBreakdown> GetExpensesByCategory(int userId, DateTime? startDate = null, DateTime? endDate = null);
}