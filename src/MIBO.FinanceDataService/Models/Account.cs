namespace MIBO.FinanceDataService.Models;

public class Account
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // checking, savings, credit
    public decimal Balance { get; set; }
    public decimal AvailableBalance { get; set; }
    public string Currency { get; set; } = "USD";
    public string Bank { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string Swift { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}