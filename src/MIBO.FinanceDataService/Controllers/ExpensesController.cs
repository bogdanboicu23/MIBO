using Microsoft.AspNetCore.Mvc;
using MIBO.FinanceDataService.Interfaces;

namespace MIBO.FinanceDataService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IFinancialService _financialService;

    public ExpensesController(IFinancialService financialService)
    {
        _financialService = financialService;
    }

    // GET: api/expenses
    [HttpGet]
    public IActionResult GetAllExpenses(
        [FromQuery] int? userId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30,
        [FromQuery] string? category = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? order = null)
    {
        var response = _financialService.GetExpenses(userId, skip, limit, category, startDate, endDate, sortBy, order);
        return Ok(new
        {
            expenses = response.Items,
            total = response.Total,
            skip = response.Skip,
            limit = response.Limit
        });
    }

    // GET: api/expenses/{id}
    [HttpGet("{id}")]
    public IActionResult GetExpenseById(int id)
    {
        var expense = _financialService.GetExpenseById(id);
        if (expense == null)
            return NotFound(new { message = $"Expense with id '{id}' not found" });

        return Ok(expense);
    }

    // GET: api/expenses/user/{userId}
    [HttpGet("user/{userId}")]
    public IActionResult GetExpensesByUser(
        int userId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30,
        [FromQuery] string? category = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var response = _financialService.GetExpenses(userId, skip, limit, category, startDate, endDate);
        return Ok(new
        {
            expenses = response.Items,
            total = response.Total,
            skip = response.Skip,
            limit = response.Limit
        });
    }

    // GET: api/expenses/categories
    [HttpGet("categories")]
    public IActionResult GetExpenseCategories()
    {
        var categories = new[]
        {
            "groceries", "dining", "transport", "shopping", "entertainment",
            "utilities", "rent", "healthcare", "education", "subscriptions", "other"
        };
        return Ok(categories);
    }
}