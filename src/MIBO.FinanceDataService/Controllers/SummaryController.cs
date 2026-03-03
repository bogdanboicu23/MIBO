using Microsoft.AspNetCore.Mvc;
using MIBO.FinanceDataService.Interfaces;

namespace MIBO.FinanceDataService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SummaryController : ControllerBase
{
    private readonly IFinancialService _financialService;

    public SummaryController(IFinancialService financialService)
    {
        _financialService = financialService;
    }

    // GET: api/summary/user/{userId}
    [HttpGet("user/{userId}")]
    public IActionResult GetUserSummary(int userId)
    {
        var summary = _financialService.GetFinancialSummary(userId);
        return Ok(summary);
    }

    // GET: api/summary/user/{userId}/balance
    [HttpGet("user/{userId}/balance")]
    public IActionResult GetUserBalance(int userId)
    {
        var summary = _financialService.GetFinancialSummary(userId);
        return Ok(new
        {
            userId = summary.UserId,
            totalBalance = summary.TotalBalance,
            currency = summary.PrimaryCurrency,
            accounts = summary.AccountBalances
        });
    }

    // GET: api/summary/user/{userId}/expenses
    [HttpGet("user/{userId}/expenses")]
    public IActionResult GetUserExpensesSummary(int userId)
    {
        var summary = _financialService.GetFinancialSummary(userId);
        return Ok(new
        {
            userId = summary.UserId,
            totalExpenses = summary.TotalExpenses,
            thisMonth = summary.ExpenseSummary.ThisMonth,
            lastMonth = summary.ExpenseSummary.LastMonth,
            average = summary.ExpenseSummary.Average,
            byCategory = summary.ExpenseSummary.ByCategory
        });
    }
}