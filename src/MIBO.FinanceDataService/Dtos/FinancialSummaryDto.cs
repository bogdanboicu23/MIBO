namespace MIBO.FinanceDataService.Dtos;

public class FinancialSummaryDto
{
    public int UserId { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public int AccountsCount { get; set; }
    public string PrimaryCurrency { get; set; } = "USD";
    public DateTime LastTransactionDate { get; set; }
    public ExpenseSummary ExpenseSummary { get; set; } = new();
    public Dictionary<string, decimal> AccountBalances { get; set; } = new();
}

public class ExpenseSummary
{
    public Dictionary<string, decimal> ByCategory { get; set; } = new();
    public decimal ThisMonth { get; set; }
    public decimal LastMonth { get; set; }
    public decimal Average { get; set; }
}