namespace MIBO.FinanceDataService.Dtos;

public class ExpenseAnalyticsDto
{
    public int UserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public List<CategoryBreakdown> ByCategory { get; set; } = new();
    public List<TimeSeriesData> TimeSeries { get; set; } = new();
    public ExpenseStatistics Statistics { get; set; } = new();
}

public class CategoryBreakdown
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public int Count { get; set; }
    public string Color { get; set; } = string.Empty; // For chart rendering
}

public class TimeSeriesData
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class ExpenseStatistics
{
    public decimal Average { get; set; }
    public decimal Median { get; set; }
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public string MostExpensiveCategory { get; set; } = string.Empty;
    public string MostFrequentCategory { get; set; } = string.Empty;
    public int TotalTransactions { get; set; }
}

public class TransactionAnalyticsDto
{
    public int UserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetFlow { get; set; }
    public string Currency { get; set; } = "USD";
    public List<TypeBreakdown> ByType { get; set; } = new();
    public List<TimeSeriesData> DailyBalance { get; set; } = new();
}

public class TypeBreakdown
{
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}