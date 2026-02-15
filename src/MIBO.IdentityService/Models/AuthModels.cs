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
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public static async Task<(bool ok, string[] errors)> VerifyAsync(
        string token,
        string secret,
        string? ip,
        HttpClient client,
        CancellationToken ct)
    {
        try
        {
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", secret),
                new KeyValuePair<string, string>("response", token),
                new KeyValuePair<string, string>("remoteip", ip ?? string.Empty)
            });

            var response = await client.PostAsync(VerifyUrl, formData, ct);

            if (!response.IsSuccessStatusCode)
            {
                return (false, new[] { $"HTTP error: {response.StatusCode}" });
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = System.Text.Json.JsonSerializer.Deserialize<TurnstileResponse>(json);

            if (result == null)
            {
                return (false, new[] { "Invalid response from Turnstile API" });
            }

            return (result.Success, result.ErrorCodes ?? Array.Empty<string>());
        }
        catch (TaskCanceledException)
        {
            return (false, new[] { "Request timeout" });
        }
        catch (HttpRequestException ex)
        {
            return (false, new[] { $"Network error: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return (false, new[] { $"Unexpected error: {ex.Message}" });
        }
    }

    private class TurnstileResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool Success { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("challenge_ts")]
        public string? ChallengeTimestamp { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("hostname")]
        public string? Hostname { get; set; }
    }
}