namespace MIBO.FinanceDataService.Models;

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = string.Empty; // debit, credit, transfer
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Merchant { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = "completed"; // pending, completed, failed
    public decimal BalanceAfter { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}