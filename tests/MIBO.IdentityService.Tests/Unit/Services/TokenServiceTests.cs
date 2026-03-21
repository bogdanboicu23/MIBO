using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using MIBO.IdentityService.Data;
using MIBO.IdentityService.Models;
using MIBO.IdentityService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace MIBO.IdentityService.Tests.Unit.Services;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly JwtSettingsOptions _jwtSettingsOptions;

    public TokenServiceTests()
    {

        _jwtSettingsOptions = new JwtSettingsOptions
        {
            AccessTokenSecret = "ThisIsAVerySecureKeyForTestingPurposes123!",
            RefreshTokenSecret = "ThisIsAnotherVerySecureKeyForRefreshTokens123!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7

        };
        _tokenService = new TokenService(
            Options.Create(_jwtSettingsOptions),
            Mock.Of<ILogger<TokenService>>());

    }

    private static ApplicationUser CreateUser(
        string id = "user-123",
        string? firstName = "Test",
        string? lastName = "User") => new()
    {
        Id = id,
        Email = "test@mibo.com",
        UserName = "testuser",
        FirstName = firstName,
        LastName = lastName
    };

    #region GenerateAccessTokenTests
    [Fact]
    public void GenerateAccessToken_ShouldReturnValidToken()
    {
        // Arrange
        var user = CreateUser("user-123");
        
        // Act 
        var token = _tokenService.GenerateAccessToken(user);
        
        // Assert
        token.Should().NotBeNullOrEmpty();
        new JwtSecurityTokenHandler().CanReadToken(token).Should().BeTrue();

        
    }


    [Fact]
    public void GenerateAccessToken_ContainsUserIdInSubclaim()
    {
        // Arrange
        var user = CreateUser("abc-123");
        
        // Act
        var token = _tokenService.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        
        // Assert
        jwt.Subject.Should().Be("abc-123");
    }


    [Fact]
    public void GenerateAccessToken_ContainsEmailClaim()
    {
        // Arrange
        var user = CreateUser();
        
        // Act
        var token = _tokenService.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        
        // Assert
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@mibo.com");
        

    }

    [Fact]
    public void GenerateAccessToken_ContainsUserNameClaim()
    {
        // Arrange
        var user = CreateUser();
        
        // Act 
        var token = _tokenService.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        
        // Assert
        jwt.Claims.Should().Contain(c => c.Type == "username" && c.Value == "testuser");
    }

    [Fact]
    public void GenerateAccessToken_IncludesFirstName_WhenPresent()
    {
        // Arrange 
        var user = CreateUser(firstName: "testuser");

        // Act
        var token = _tokenService.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        jwt.Claims.Should().Contain(c => c.Type == "first_name" && c.Value == "testuser");
    }

    [Fact]
    public void GenerateAccessToken_ExcludesFirstName_WhenNull()
    {
        // Arrange 
        var user = CreateUser(firstName: null);
        
        // Act
        var token = _tokenService.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        
        // Assert
        jwt.Claims.Should().NotContain(c => c.Type == "first_name");
        
    }

    [Fact]
    public void GenerateAccessToken_ExcludesLastName_WhenNull()
    {
        // Arrange
        var user = CreateUser(lastName: null);
        
        // Act
        var token = _tokenService.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        
        // Assert
        jwt.Claims.Should().NotContain(c => c.Type == "last_name");
    }

    [Fact]
    public void GenerateAccessToken_SetsCorrectIssuerAndAudience()
    {
        // Arrange
        var user = CreateUser("user-123");
        
        // Act
        var token = _tokenService.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        
        // Assert
        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateAccessToken_ExpiresInConfiguredMinutes()
    {
        // Arrange
        var user = CreateUser("user-123");
        var before = DateTime.UtcNow.AddMinutes(_jwtSettingsOptions.AccessTokenExpirationMinutes);
        
        // Act
        var token = _tokenService.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        
        var after = DateTime.UtcNow.AddMinutes(_jwtSettingsOptions.AccessTokenExpirationMinutes);
        
        // Assert
        jwt.ValidTo.Should().BeOnOrAfter(before.AddSeconds(-1));
        jwt.ValidTo.Should().BeOnOrBefore(after.AddSeconds(1));
    }
    
    #endregion
    
    
    #region GenerateRefreshTokenTests

    [Fact]
    public void GenerateRefreshToken_ReturnsValidBase64String()
    {
        // Act
        var token = _tokenService.GenerateRefreshToken();
        
        // Assert
        token.Should().NotBeNullOrEmpty();

        var act = () => Convert.FromBase64String(token);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateRefreshToken_Returns32BytesWhenDecoded()
    {
        // Act 
        var token = _tokenService.GenerateRefreshToken();
        var bytes = Convert.FromBase64String(token);
        
        // Assert
        bytes.Should().HaveCount(32);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsDifferentValueEachCall()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();
        
        // Assert
        token1.Should().NotBe(token2);
    }
    #endregion
    
    #region ValidateTokenTests

    [Fact]
    public void ValidateToken_ValidToken_ReturnsPrincipalWithClaims()
    {
        // Arrange
        var user = CreateUser("user-123");
        var token = _tokenService.GenerateAccessToken(user);
        
        // Act
        var principal = _tokenService.ValidateToken(token);
        
        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier).Value.Should().Be("user-123");
    }

    [Fact]
    public void ValidateToken_GarbageString_ReturnsNull()
    {
        // Act
        var principal = _tokenService.ValidateToken("this-is-not-a-valid-token");
        
        
        // Assert
        principal.Should().BeNull();

    }


    [Fact]
    public void ValidateToken_TokenSignedWithDifferentKey_ReturnsNull()
    {
        // Arrange 
        var wrongKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("ACompletelyDifferentSecretKeyForTesting1234!"));
        var creds = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256);
        
        var jwt = new JwtSecurityToken(
            issuer: _jwtSettingsOptions.Issuer,
            audience: _jwtSettingsOptions.Audience,
            claims: new [] { new Claim(ClaimTypes.NameIdentifier, "validate-me") },
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );
        
        var forgedToken = new JwtSecurityTokenHandler().WriteToken(jwt);
        
        // Act
        var principal = _tokenService.ValidateToken(forgedToken);
        
        // Assert
        principal.Should().BeNull();
        
    }

    [Fact]
    public void ValidateToken_WrongIssuer_ReturnsNull()
    {
        // Arrange
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettingsOptions.AccessTokenSecret));
        
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: "WrongIssuer",
            audience: _jwtSettingsOptions.Audience,
            claims: new[] { new Claim("sub", "validate-me") },
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );
        
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        
        // Act 
        var principal = _tokenService.ValidateToken(token);
        
        // Assert
        principal.Should().BeNull();
    }
    
    #endregion
    
}