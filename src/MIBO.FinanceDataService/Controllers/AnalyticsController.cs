using Microsoft.AspNetCore.Mvc;
using MIBO.FinanceDataService.Interfaces;

namespace MIBO.FinanceDataService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IFinancialService _financialService;

    public AnalyticsController(IFinancialService financialService)
    {
        _financialService = financialService;
    }

    // GET: api/analytics/expenses/user/{userId}
    [HttpGet("expenses/user/{userId}")]
    public IActionResult GetExpenseAnalytics(
        int userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? period = null)
    {
        var analytics = _financialService.GetExpenseAnalytics(userId, startDate, endDate, period);
        return Ok(analytics);
    }

    // GET: api/analytics/expenses/user/{userId}/categories
    [HttpGet("expenses/user/{userId}/categories")]
    public IActionResult GetExpensesByCategory(
        int userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? period = null)
    {
        var analytics = _financialService.GetExpenseAnalytics(userId, startDate, endDate, period);
        return Ok(new
        {
            userId = analytics.UserId,
            startDate = analytics.StartDate,
            endDate = analytics.EndDate,
            totalAmount = analytics.TotalAmount,
            currency = analytics.Currency,
            categories = analytics.ByCategory,
            chartData = new
            {
                labels = analytics.ByCategory.Select(c => c.Category).ToArray(),
                values = analytics.ByCategory.Select(c => c.Amount).ToArray(),
                percentages = analytics.ByCategory.Select(c => c.Percentage).ToArray(),
                colors = analytics.ByCategory.Select(c => c.Color).ToArray()
            }
        });
    }

    // GET: api/analytics/transactions/user/{userId}
    [HttpGet("transactions/user/{userId}")]
    public IActionResult GetTransactionAnalytics(
        int userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? period = null)
    {
        if (!string.IsNullOrEmpty(period))
        {
            var now = DateTime.Now;
            switch (period?.ToLower())
            {
                case "lastmonth":
                    startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    endDate = new DateTime(now.Year, now.Month, 1).AddSeconds(-1);
                    break;
                case "thismonth":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    endDate = now;
                    break;
                case "last30days":
                    startDate = now.Date.AddDays(-30);
                    endDate = now;
                    break;
            }
        }

        var analytics = _financialService.GetTransactionAnalytics(userId, startDate, endDate);
        return Ok(analytics);
    }

    // GET: api/analytics/expenses/user/{userId}/statistics
    [HttpGet("expenses/user/{userId}/statistics")]
    public IActionResult GetExpenseStatistics(
        int userId,
        [FromQuery] string? period = null)
    {
        var analytics = _financialService.GetExpenseAnalytics(userId, null, null, period);
        return Ok(analytics.Statistics);
    }

    // GET: api/analytics/expenses/user/{userId}/timeseries
    [HttpGet("expenses/user/{userId}/timeseries")]
    public IActionResult GetExpenseTimeSeries(
        int userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? period = null)
    {
        var analytics = _financialService.GetExpenseAnalytics(userId, startDate, endDate, period);
        return Ok(new
        {
            userId = analytics.UserId,
            startDate = analytics.StartDate,
            endDate = analytics.EndDate,
            timeSeries = analytics.TimeSeries,
            chartData = new
            {
                labels = analytics.TimeSeries.Select(t => t.Date.ToString("yyyy-MM-dd")).ToArray(),
                values = analytics.TimeSeries.Select(t => t.Amount).ToArray()
            }
        });
    }

    // GET: api/analytics/periods
    [HttpGet("periods")]
    public IActionResult GetAvailablePeriods()
    {
        var periods = new[]
        {
            new { value = "today", label = "Today" },
            new { value = "yesterday", label = "Yesterday" },
            new { value = "thisweek", label = "This Week" },
            new { value = "lastweek", label = "Last Week" },
            new { value = "thismonth", label = "This Month" },
            new { value = "lastmonth", label = "Last Month" },
            new { value = "last30days", label = "Last 30 Days" },
            new { value = "last90days", label = "Last 90 Days" },
            new { value = "thisyear", label = "This Year" },
            new { value = "lastyear", label = "Last Year" }
        };
        return Ok(periods);
    }
}