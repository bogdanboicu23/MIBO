using System.ComponentModel.DataAnnotations;

namespace MIBO.IdentityService.Models;

public class RegisterModel
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

public class LoginModel
{
    [Required]
    public required string UsernameOrEmail { get; set; }

    [Required]
    public required string Password { get; set; }
}

public class AuthResponse
{
    public required string Token { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public required UserInfo User { get; set; }
}

public class UserInfo
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public required string Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class RefreshTokenModel
{
    [Required]
    public required string Token { get; set; }

    [Required]
    public required string RefreshToken { get; set; }
}