using Microsoft.AspNetCore.Mvc;
using MIBO.FinanceDataService.Interfaces;

namespace MIBO.FinanceDataService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly IFinancialService _financialService;

    public TransactionsController(IFinancialService financialService)
    {
        _financialService = financialService;
    }

    // GET: api/transactions
    [HttpGet]
    public IActionResult GetAllTransactions(
        [FromQuery] int? userId,
        [FromQuery] int? accountId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30,
        [FromQuery] string? type = null,
        [FromQuery] string? category = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? order = null)
    {
        var response = _financialService.GetTransactions(userId, accountId, skip, limit, type, category, startDate, endDate, sortBy, order);
        return Ok(new
        {
            transactions = response.Items,
            total = response.Total,
            skip = response.Skip,
            limit = response.Limit
        });
    }

    // GET: api/transactions/{id}
    [HttpGet("{id}")]
    public IActionResult GetTransactionById(int id)
    {
        var transaction = _financialService.GetTransactionById(id);
        if (transaction == null)
            return NotFound(new { message = $"Transaction with id '{id}' not found" });

        return Ok(transaction);
    }

    // GET: api/transactions/user/{userId}
    [HttpGet("user/{userId}")]
    public IActionResult GetTransactionsByUser(
        int userId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30,
        [FromQuery] string? type = null,
        [FromQuery] string? category = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var response = _financialService.GetTransactions(userId, null, skip, limit, type, category, startDate, endDate);
        return Ok(new
        {
            transactions = response.Items,
            total = response.Total,
            skip = response.Skip,
            limit = response.Limit
        });
    }

    // GET: api/transactions/account/{accountId}
    [HttpGet("account/{accountId}")]
    public IActionResult GetTransactionsByAccount(
        int accountId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30)
    {
        var response = _financialService.GetTransactions(null, accountId, skip, limit);
        return Ok(new
        {
            transactions = response.Items,
            total = response.Total,
            skip = response.Skip,
            limit = response.Limit
        });
    }

    // GET: api/transactions/search
    [HttpGet("search")]
    public IActionResult SearchTransactions(
        [FromQuery] string q,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Search query 'q' is required" });

        var response = _financialService.SearchTransactions(q, skip, limit);
        return Ok(new
        {
            transactions = response.Items,
            total = response.Total,
            skip = response.Skip,
            limit = response.Limit
        });
    }
}