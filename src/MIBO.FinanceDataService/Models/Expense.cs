namespace MIBO.FinanceDataService.Models;

public class Expense
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TransactionId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string Notes { get; set; } = string.Empty;
}