using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MIBO.IdentityService.Data;
using MIBO.IdentityService.Models;
using MIBO.IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MIBO.IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authenticationService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authenticationService,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

   [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserDto data, CancellationToken ct)
    {
        // Check if Turnstile is enabled
        var turnstileEnabled = _config.GetValue<bool>("Turnstile:Enabled", true);

        if (turnstileEnabled)
        {
            if (string.IsNullOrWhiteSpace(data.TurnstileToken))
                return BadRequest(new { message = "Captcha verification required" });

            var secret = _config["Turnstile:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                // Log this as a critical error in production
                return StatusCode(500, new { message = "Turnstile configuration error" });
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var http = _httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(10); // Set timeout for Turnstile verification

            var (ok, errors) = await TurnstileVerifier.VerifyAsync(data.TurnstileToken, secret, ip, http, ct);
            if (!ok)
            {
                var errorMessage = errors?.Length > 0 ? string.Join(", ", errors) : "Captcha verification failed";
                _logger.LogWarning("Turnstile verification failed for IP {IP}: {Errors}", ip, errorMessage);
                return Unauthorized(new { message = "Captcha verification failed", details = errorMessage });
            }

            _logger.LogInformation("Turnstile verification successful for IP {IP}", ip);
        }

        var userDto = new UserDto
        {
            Email = data.Email,
            Password = data.Password
        };

        var result = await _authenticationService.UserLogin(userDto);
        if (string.IsNullOrEmpty(result.AccessToken)) return BadRequest("Login Failed");

        SetAuthCookies(result.RefreshToken);
        return Ok(new { jwtToken = result.AccessToken });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userDto = new RegisterDto()
        {
            Email = model.Email,
            Password = model.Password,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Username = model.Username
        };

        var result = await _authenticationService.UserSignUp(userDto);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(request.JwtToken))
        {
            return Unauthorized("Token Refresh Failed");
        }

        var result = await _authenticationService.RefreshToken(new TokenDto
        {
            AccessToken = request.JwtToken,
            RefreshToken = refreshToken
        });

        if (string.IsNullOrEmpty(result.AccessToken)) return Unauthorized("Token Refresh Failed");

        SetAuthCookies(result.RefreshToken);
        return Ok(new { jwtToken = result.AccessToken });
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new {
            message = "Authentication service is running",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    [HttpGet("test-auth")]
    [Authorize]
    public IActionResult TestAuth()
    {
        var userId = User.Identity?.Name ?? "Unknown";
        return Ok(new {
            message = $"Hello {userId}, you are authenticated!",
            timestamp = DateTime.UtcNow,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userInfo = new UserInfo
        {
            Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                 ?? string.Empty,
            Email = User.FindFirst(ClaimTypes.Email)?.Value
                    ?? User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? string.Empty,
            Username = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
            FirstName = User.FindFirst("firstName")?.Value,
            LastName = User.FindFirst("lastName")?.Value
        };

        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new {
            user = userInfo,
            roles = roles
        });
    }

    private void SetAuthCookies(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Secure = true,
			Domain = "mibo.monster",
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}