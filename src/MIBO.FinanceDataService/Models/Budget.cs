namespace MIBO.FinanceDataService.Models;

public class Budget
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public decimal Spent { get; set; }
    public decimal Remaining => Limit - Spent;
    public string Period { get; set; } = "monthly"; // daily, weekly, monthly, yearly
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}