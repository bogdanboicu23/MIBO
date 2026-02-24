using Microsoft.AspNetCore.Mvc;
using MIBO.FinanceDataService.Interfaces;

namespace MIBO.FinanceDataService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IFinancialService _financialService;

    public AccountsController(IFinancialService financialService)
    {
        _financialService = financialService;
    }

    // GET: api/accounts
    [HttpGet]
    public IActionResult GetAllAccounts(
        [FromQuery] int? userId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30,
        [FromQuery] string? select = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? order = null)
    {
        var response = _financialService.GetAccounts(userId, skip, limit, select, sortBy, order);
        return Ok(new
        {
            accounts = response.Items,
            total = response.Total,
            skip = response.Skip,
            limit = response.Limit
        });
    }

    // GET: api/accounts/{id}
    [HttpGet("{id}")]
    public IActionResult GetAccountById(int id)
    {
        var account = _financialService.GetAccountById(id);
        if (account == null)
            return NotFound(new { message = $"Account with id '{id}' not found" });

        return Ok(account);
    }

    // GET: api/accounts/user/{userId}
    [HttpGet("user/{userId}")]
    public IActionResult GetAccountsByUser(
        int userId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30)
    {
        var response = _financialService.GetAccounts(userId, skip, limit);
        return Ok(new
        {
            accounts = response.Items,
            total = response.Total,
            skip = response.Skip,
            limit = response.Limit
        });
    }

    // GET: api/accounts/number/{accountNumber}
    [HttpGet("number/{accountNumber}")]
    public IActionResult GetAccountByNumber(string accountNumber)
    {
        var account = _financialService.GetAccountByNumber(accountNumber);
        if (account == null)
            return NotFound(new { message = $"Account with number '{accountNumber}' not found" });

        return Ok(account);
    }
}