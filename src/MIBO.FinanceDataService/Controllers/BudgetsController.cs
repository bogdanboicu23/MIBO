using Microsoft.AspNetCore.Mvc;
using MIBO.FinanceDataService.Interfaces;

namespace MIBO.FinanceDataService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BudgetsController : ControllerBase
{
    private readonly IFinancialService _financialService;

    public BudgetsController(IFinancialService financialService)
    {
        _financialService = financialService;
    }

    // GET: api/budgets
    [HttpGet]
    public IActionResult GetAllBudgets(
        [FromQuery] int? userId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30,
        [FromQuery] string? category = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? period = null)
    {
        var response = _financialService.GetBudgets(userId, skip, limit, category, isActive, period);
        return Ok(new
        {
            budgets = response.Items,
            total = response.Total,
            skip = response.Skip,
            limit = response.Limit
        });
    }

    // GET: api/budgets/{id}
    [HttpGet("{id}")]
    public IActionResult GetBudgetById(int id)
    {
        var budget = _financialService.GetBudgetById(id);
        if (budget == null)
            return NotFound(new { message = $"Budget with id '{id}' not found" });

        return Ok(budget);
    }

    // GET: api/budgets/user/{userId}
    [HttpGet("user/{userId}")]
    public IActionResult GetBudgetsByUser(
        int userId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30,
        [FromQuery] bool? isActive = null)
    {
        var response = _financialService.GetBudgets(userId, skip, limit, null, isActive);
        return Ok(new
        {
            budgets = response.Items,
            total = response.Total,
            skip = response.Skip,
            limit = response.Limit
        });
    }
}