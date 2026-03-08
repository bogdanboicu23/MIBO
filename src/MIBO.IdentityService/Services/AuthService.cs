using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MIBO.IdentityService.Data;
using MIBO.IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MIBO.IdentityService.Services;

public interface IAuthService
{
    Task<LoginResponse> UserLogin(UserDto data, CancellationToken ct = default);
    Task<ApiResponse> UserSignUp(RegisterDto data, CancellationToken ct = default);
    Task<LoginResponse> RefreshToken(TokenDto data, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettingsOptions _jwtSettings;

    public AuthService(UserManager<ApplicationUser> userManager, IOptions<JwtSettingsOptions> jwtSettings)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<LoginResponse> UserLogin(UserDto data, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(data.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, data.Password))
            return new LoginResponse { };
        return await GenerateLoginResponse(user, ct);
    }

    public async Task<ApiResponse> UserSignUp(RegisterDto data, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = data.Username,
            Email = data.Email,
            FirstName = data.FirstName,
            LastName = data.LastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, data.Password);

        if (!result.Succeeded)
            return ApiResponse.Fail(string.Join("; ", result.Errors.Select(e => e.Description)));

        // Optionally assign default role
        // await _userManager.AddToRoleAsync(user, "User");

        return ApiResponse.Ok();
    }

    public async Task<LoginResponse> RefreshToken(TokenDto data, CancellationToken ct = default)
    {
        var jwtClaimsPrincipal = GetTokenPrincipal(data.AccessToken);

        var userIdClaim = jwtClaimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)
                          ?? jwtClaimsPrincipal?.FindFirst(JwtRegisteredClaimNames.Sub);

        if (userIdClaim?.Value is null)
            return new LoginResponse { };

        var user = await _userManager.FindByIdAsync(userIdClaim.Value);

        if (user is null || user.RefreshToken != data.RefreshToken || IsJwtExpired(data.RefreshToken))
            return new LoginResponse { };

        return await GenerateLoginResponse(user, ct);
    }

    private async Task<LoginResponse> GenerateLoginResponse(ApplicationUser user, CancellationToken ct = default)
    {
        var accessTokenKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.AccessTokenSecret));
        var accessTokenExpires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);

        // Generate access token with user claims
        var accessToken = GenerateToken(accessTokenKey, accessTokenExpires, user, roles);

        var refreshTokenKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.RefreshTokenSecret));
        var refreshTokenExpires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        // Generate refresh token with minimal claims
        var refreshToken = user.RefreshToken != null && !IsJwtExpired(user.RefreshToken)
            ? user.RefreshToken
            : GenerateRefreshToken(refreshTokenKey, refreshTokenExpires, user.Id);

        user.RefreshToken = refreshToken;
        user.LastLoginAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    private string GenerateToken(SymmetricSecurityKey key, DateTime expires, ApplicationUser user, IList<string> roles)
    {
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim("firstName", user.FirstName ?? string.Empty),
            new Claim("lastName", user.LastName ?? string.Empty)
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add default role if no roles assigned
        if (!roles.Any())
        {
            claims.Add(new Claim(ClaimTypes.Role, "User"));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken(SymmetricSecurityKey key, DateTime expires, string userId)
    {
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal? GetTokenPrincipal(string token)
    {
        try
        {
            var validation = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false,

                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwtSettings.AccessTokenSecret)
                )
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, validation, out _);
        }
        catch (Exception ex)
        {

            return null;
        }
    }


    private bool IsJwtExpired(string token)
    {
        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(token))
            throw new ArgumentException("Invalid JWT token");

        var jwtToken = handler.ReadJwtToken(token);

        var exp = jwtToken.ValidTo;

        return exp < DateTime.UtcNow;
    }
}
