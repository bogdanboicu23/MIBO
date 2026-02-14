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
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        ApplicationDbContext context,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        // Check if user exists
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "User with this email already exists" });
        }

        existingUser = await _userManager.FindByNameAsync(model.Username);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Username is already taken" });
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = model.Username,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Save refresh token (you'll need to add a RefreshTokens table)
        // For now, we'll store it in user's SecurityStamp
        user.SecurityStamp = refreshToken;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {Username} registered successfully", user.UserName);

        return Ok(new AuthResponse
        {
            Message = "User created successfully",
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email!,
                Username = user.UserName!,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        });
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "Auth service is working 🚀" });
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        // Find user by email or username
        ApplicationUser? user;
        if (model.UsernameOrEmail.Contains('@'))
        {
            user = await _userManager.FindByEmailAsync(model.UsernameOrEmail);
        }
        else
        {
            user = await _userManager.FindByNameAsync(model.UsernameOrEmail);
        }

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // Verify password
        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Save refresh token
        user.SecurityStamp = refreshToken;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {Username} logged in successfully", user.UserName);

        return Ok(new AuthResponse
        {
            Message = "Auth service reached succesfully",
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email!,
                Username = user.UserName!,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel model)
    {
        var principal = _tokenService.ValidateToken(model.Token);
        if (principal == null)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.SecurityStamp != model.RefreshToken)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }

        // Generate new tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Save new refresh token
        user.SecurityStamp = refreshToken;
        await _userManager.UpdateAsync(user);

        return Ok(new AuthResponse
        {
            Message = "Auth service reached succesfully",
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email!,
                Username = user.UserName!,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Invalidate refresh token
                user.SecurityStamp = Guid.NewGuid().ToString();
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("User {Username} logged out", user.UserName);
            }
        }

        return Ok(new { message = "Logged out successfully" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new UserInfo
        {
            Id = user.Id,
            Email = user.Email!,
            Username = user.UserName!,
            FirstName = user.FirstName,
            LastName = user.LastName
        });
    }
}