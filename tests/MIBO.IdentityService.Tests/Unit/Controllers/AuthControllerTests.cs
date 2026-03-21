using System.Security.Claims;
using FluentAssertions;
using MIBO.IdentityService.Controllers;
using MIBO.IdentityService.Models;
using MIBO.IdentityService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MIBO.IdentityService.Tests.Unit.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Dictionary<string, string?> _configData;
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _configData = new Dictionary<string, string?>
        {
            ["Turnstile:Enabled"] = "false",
            ["Turnstile:SecretKey"] = "test-secret"
        };
        
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(_configData)
            .Build();
        
        _sut = new AuthController(
            _authServiceMock.Object,
            _httpClientFactoryMock.Object,
            config,
            _loggerMock.Object);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private AuthController CreateControllerWithConfig(Dictionary<string, string?> configOverrides)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configOverrides)
            .Build();
        
        var controller = new AuthController(
            _authServiceMock.Object,
            _httpClientFactoryMock.Object,
            config,
            _loggerMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        
        return controller;
    }

    private void SetAuthenticatedUser(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuth");
        
        _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
    }
    
    #region LOGIN - Turnstile Disabled


    [Fact]
    public async Task Login_TurnstileDisabled_ValidCredentials_ReturnsOkWithJwt()
    {
        // Arrange 
        _authServiceMock
            .Setup(x =>
                x.UserLogin(It.IsAny<UserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse
            {
                AccessToken = "fake-jwt",
                RefreshToken = "fake-refresh"
            });

        var dto = new UserDto
        {
            Email = "a@b.com",
            Password = "pass"
        };
        
        // Act
        var result = await _sut.Login(dto, CancellationToken.None);
        
        // 
        
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;

        ok.Value.Should().BeEquivalentTo(new { jwtToken = "fake-jwt" });
        
    }

    [Fact]
    public async Task Login_TurnstileDisabled_InvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.UserLogin(It.IsAny<UserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse());

        var dto = new UserDto
        {
            Email = "a@b.com",
            Password = "wrong"
        };
        
        // Act
        var result = await _sut.Login(dto, CancellationToken.None);
        
        // Assert 
        result.Should().BeOfType<BadRequestObjectResult>();
    }
    
    
    #endregion
    
    #region LOGIN - Turnstile Enabled


    [Fact]
    public async Task Login_TurnstileEnabled_MissingToken_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateControllerWithConfig( new()
        {
            ["Turnstile:Enabled"] = "true",
            ["Turnstile:SecretKey"] = "secret"
        });

        var dto = new UserDto
        {
            Email = "a@b.com",
            Password = "pass",
            TurnstileToken = null
        };
        
        // Act 
        var result = await controller.Login(dto, CancellationToken.None);
        
        // Assert
        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().BeEquivalentTo(new { message = "Captcha verification required" });
    }

    [Fact]
    public async Task Login_TurnstileEnabled_MissingSecretKey_Returns500()
    {
        // Arrange
        var controller = CreateControllerWithConfig(new()
        {
            ["Turnstile:Enabled"] = "true",
            ["Turnstile:SecretKey"] = ""
        });

        var dto = new UserDto
        {
            Email = "a@b.com",
            Password = "pass",
            TurnstileToken = "token"
        };
        
        // Act
        var result = await controller.Login(dto, CancellationToken.None);
        
        
        // Assert 
        var status = result.Should().BeOfType<ObjectResult>().Subject;
        status.StatusCode.Should().Be(500);
    }
    
    #endregion

    #region REGISTER

    [Fact]
    public async Task Register_ValidData_ReturnsOk()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.UserSignUp(It.IsAny<RegisterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse.Ok());

        var dto = new RegisterDto
        {
            Email = "new@test.com",
            Password = "Pass123!",
            Username = "newuser"
        };

        // Act
        var result = await _sut.Register(dto, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { message = "User registered successfully" });
    }

    [Fact]
    public async Task Register_ServiceFails_ReturnsBadRequestWithMessage()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.UserSignUp(It.IsAny<RegisterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse.Fail("Email is already taken"));

        var dto = new RegisterDto
        {
            Email = "dup@test.com",
            Password = "Pass123!",
            Username = "dup"
        };

        // Act
        var result = await _sut.Register(dto, CancellationToken.None);

        // Assert
        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().BeEquivalentTo(new { message = "Email is already taken" });
    }

    [Fact]
    public async Task Register_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange — manually add a model error (normally the framework does this)
        _sut.ModelState.AddModelError("Email", "Email is required");

        var dto = new RegisterDto
        {
            Email = "",
            Password = "Pass123!",
            Username = "user"
        };

        // Act
        var result = await _sut.Register(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region REFRESH TOKEN

    [Fact]
    public async Task RefreshToken_MissingCookie_ReturnsUnauthorized()
    {
        // Arrange — no "refreshToken" cookie in the request
        var request = new RefreshTokenRequest { JwtToken = "some-jwt" };

        // Act
        var result = await _sut.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task RefreshToken_MissingJwtToken_ReturnsUnauthorized()
    {
        // Arrange — cookie exists but JwtToken is empty
        _sut.ControllerContext.HttpContext.Request.Headers["Cookie"] = "refreshToken=abc";

        var request = new RefreshTokenRequest { JwtToken = "" };

        // Act
        var result = await _sut.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task RefreshToken_Valid_ReturnsOkWithNewJwt()
    {
        // Arrange — set the refreshToken cookie
        _sut.ControllerContext.HttpContext.Request.Headers["Cookie"] = "refreshToken=valid-refresh";

        _authServiceMock
            .Setup(x => x.RefreshToken(It.IsAny<TokenDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse
            {
                AccessToken = "new-jwt",
                RefreshToken = "new-refresh"
            });

        var request = new RefreshTokenRequest { JwtToken = "old-jwt" };

        // Act
        var result = await _sut.RefreshToken(request, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { jwtToken = "new-jwt" });
    }

    [Fact]
    public async Task RefreshToken_ServiceReturnsEmpty_ReturnsUnauthorized()
    {
        // Arrange
        _sut.ControllerContext.HttpContext.Request.Headers["Cookie"] = "refreshToken=old";

        _authServiceMock
            .Setup(x => x.RefreshToken(It.IsAny<TokenDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse()); // AccessToken is null

        var request = new RefreshTokenRequest { JwtToken = "old-jwt" };

        // Act
        var result = await _sut.RefreshToken(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region TEST (health check)

    [Fact]
    public void Test_ReturnsOkWithServiceInfo()
    {
        // Act
        var result = _sut.Test();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            message = "Authentication service is running",
            version = "1.0.0"
        }, options => options.Excluding(x => x.Name == "timestamp"));
    }

    #endregion

    #region TEST-AUTH

    [Fact]
    public void TestAuth_AuthenticatedUser_ReturnsGreeting()
    {
        // Arrange — simulate an authenticated user with Name = "bob"
        SetAuthenticatedUser(
            new Claim(ClaimTypes.Name, "bob"));

        // Act
        var result = _sut.TestAuth();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            message = "Hello bob, you are authenticated!"
        }, options => options
            .Excluding(x => x.Name == "timestamp")
            .Excluding(x => x.Name == "claims"));
    }

    #endregion

    #region ME (GetCurrentUser)

    [Fact]
    public void GetCurrentUser_ReturnsUserInfoFromClaims()
    {
        // Arrange — set up a ClaimsPrincipal with all the claims the method reads
        SetAuthenticatedUser(
            new Claim(ClaimTypes.NameIdentifier, "user-42"),
            new Claim(ClaimTypes.Email, "me@mibo.com"),
            new Claim(ClaimTypes.Name, "myusername"),
            new Claim("firstName", "John"),
            new Claim("lastName", "Doe"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "User"));

        // Act
        var result = _sut.GetCurrentUser();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            user = new
            {
                Id = "user-42",
                Email = "me@mibo.com",
                Username = "myusername",
                FirstName = "John",
                LastName = "Doe"
            },
            roles = new[] { "Admin", "User" }
        });
    }

    [Fact]
    public void GetCurrentUser_MissingClaims_ReturnsEmptyStrings()
    {
        // Arrange — no claims at all
        SetAuthenticatedUser();

        // Act
        var result = _sut.GetCurrentUser();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            user = new
            {
                Id = "",
                Email = "",
                Username = "",
                FirstName = (string?)null,
                LastName = (string?)null
            },
            roles = Array.Empty<string>()
        });
    }

    #endregion
}