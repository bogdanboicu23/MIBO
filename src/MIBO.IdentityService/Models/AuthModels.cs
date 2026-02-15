using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MIBO.IdentityService.Models;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [MinLength(6)]
    public required string Password { get; set; }

    [Required]
    public required string Username { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}


public class UserInfo
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public required string Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}


public class UserDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }

    public string? TurnstileToken { get; set; }
}

public class TokenDto
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}

public class LoginResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}

public class RefreshTokenRequest
{
    [Required]
    public required string JwtToken { get; set; }
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static ApiResponse Ok() => new ApiResponse { Success = true };
    public static ApiResponse Fail(string message) => new ApiResponse { Success = false, Message = message };
}


public static class TurnstileVerifier
{
    public static async Task<(bool ok, string[] errors)> VerifyAsync(
        string token,
        string secret,
        string? ip,
        HttpClient client,
        CancellationToken ct)
    {
        // This is a placeholder - you'll need to implement the actual Turnstile verification
        // For now, returning true to allow testing
        await Task.Delay(1, ct);
        return (true, Array.Empty<string>());
    }
}