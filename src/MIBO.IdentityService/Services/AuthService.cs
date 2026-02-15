using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MIBO.IdentityService.Data;
using MIBO.IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace MIBO.IdentityService.Services;

public interface IAuthService
{
    Task<LoginResponse> UserLogin(UserDto data);
    Task<ApiResponse> UserSignUp(RegisterDto data);
    Task<LoginResponse> RefreshToken(TokenDto data);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<LoginResponse> UserLogin(UserDto data)
    {
        var user = await _userManager.FindByEmailAsync(data.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, data.Password))
            return new LoginResponse { };
        return await GenerateLoginResponse(user);
    }

    public async Task<ApiResponse> UserSignUp(RegisterDto data)
    {
        var user = new ApplicationUser
        {
            UserName = data.Username,
            Email = data.Email
        };

        var result = await _userManager.CreateAsync(user, data.Password);

        if (!result.Succeeded)
            return ApiResponse.Fail(string.Join("; ", result.Errors.Select(e => e.Description)));

        return ApiResponse.Ok();
    }

    public async Task<LoginResponse> RefreshToken(TokenDto data)
    {
        var jwtClaimsPrincipal = GetTokenPrincipal(data.AccessToken);

        if (jwtClaimsPrincipal?.Identity?.Name is null)
            return new LoginResponse { };

        var user = await _userManager.FindByNameAsync(jwtClaimsPrincipal.Identity.Name);

        if (user is null || user.RefreshToken != data.RefreshToken || IsJwtExpired(data.RefreshToken))
            return new LoginResponse { };

        return await GenerateLoginResponse(user);
    }

    private async Task<LoginResponse> GenerateLoginResponse(ApplicationUser user)
    {
        var accessTokenKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:AccessTokenSecret"] ??
                                                            throw new InvalidOperationException()));
        var accessTokenExpires = DateTime.UtcNow.AddMinutes(int.Parse(
            _configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? throw new InvalidOperationException()));
        var accessToken = GenerateToken(accessTokenKey, accessTokenExpires);

        var refreshTokenKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:RefreshTokenSecret"] ??
                                                            throw new InvalidOperationException()));
        var refreshTokenExpires = DateTime.UtcNow.AddDays(int.Parse(
            _configuration["JwtSettings:RefreshTokenExpirationDays"] ?? throw new InvalidOperationException()));
        var refreshToken = user.RefreshToken != null && !IsJwtExpired(user.RefreshToken)
            ? user.RefreshToken
            : GenerateToken(refreshTokenKey, refreshTokenExpires);

        user.RefreshToken = refreshToken;

        await _userManager.UpdateAsync(user);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    private string GenerateToken(SymmetricSecurityKey key, DateTime expires)
    {
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Name, "Admin")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
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

                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidAudience = _configuration["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        _configuration["JwtSettings:AccessTokenSecret"]
                        ?? throw new InvalidOperationException("AccessTokenSecret is missing")
                    )
                )
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, validation, out _);
        }
        catch (Exception ex)
        {
            // ar fi bine să loghezi ex.Message aici ca să vezi exact problema
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