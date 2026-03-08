using System.ComponentModel.DataAnnotations;

namespace MIBO.IdentityService.Models;

public sealed class JwtSettingsOptions
{
    public const string SectionName = "JwtSettings";

    [Required]
    public string AccessTokenSecret { get; set; } = string.Empty;

    [Required]
    public string RefreshTokenSecret { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 60 * 24)]
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
