using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MIBO.IdentityService.Services;
using MIBO.IdentityService.Data;
using MIBO.IdentityService.Models;
using System.Collections.Generic;

namespace MIBO.IdentityService.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        // Setup UserManager mock
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null, null, null, null, null, null, null, null);

        // Setup Configuration
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(x => x["JwtSettings:AccessTokenSecret"]).Returns("ThisIsAVerySecureKeyForTestingPurposes123!");
        _configurationMock.Setup(x => x["JwtSettings:RefreshTokenSecret"]).Returns("ThisIsAnotherVerySecureKeyForRefreshTokens123!");
        _configurationMock.Setup(x => x["JwtSettings:AccessTokenExpirationMinutes"]).Returns("15");
        _configurationMock.Setup(x => x["JwtSettings:RefreshTokenExpirationDays"]).Returns("7");
        _configurationMock.Setup(x => x["JwtSettings:Issuer"]).Returns("TestIssuer");
        _configurationMock.Setup(x => x["JwtSettings:Audience"]).Returns("TestAudience");

        _sut = new AuthService(_userManagerMock.Object, _configurationMock.Object);
    }

    [Fact]
    public async Task UserLogin_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        var email = "test@mibo.com";
        var password = "Test123!";
        var user = new ApplicationUser
        {
            Email = email,
            UserName = "testuser",
            Id = Guid.NewGuid().ToString(),
            FirstName = "Test",
            LastName = "User"
        };

        var userDto = new UserDto
        {
            Email = email,
            Password = password
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, password))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        _userManagerMock
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.UserLogin(userDto);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UserLogin_WithInvalidEmail_ReturnsEmptyResponse()
    {
        // Arrange
        var email = "invalid@mibo.com";
        var password = "Test123!";

        var userDto = new UserDto
        {
            Email = email,
            Password = password
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _sut.UserLogin(userDto);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().BeNullOrEmpty();
        result.RefreshToken.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task UserLogin_WithIncorrectPassword_ReturnsEmptyResponse()
    {
        // Arrange
        var email = "test@mibo.com";
        var password = "WrongPassword";
        var user = new ApplicationUser
        {
            Email = email,
            UserName = "testuser",
            Id = Guid.NewGuid().ToString()
        };

        var userDto = new UserDto
        {
            Email = email,
            Password = password
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, password))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.UserLogin(userDto);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().BeNullOrEmpty();
        result.RefreshToken.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task UserSignUp_WithValidData_CreatesUserSuccessfully()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "newuser@mibo.com",
            Password = "Test123!",
            Username = "newuser",
            FirstName = "New",
            LastName = "User"
        };

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.UserSignUp(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();

        _userManagerMock.Verify(x => x.CreateAsync(
            It.Is<ApplicationUser>(u =>
                u.Email == registerDto.Email &&
                u.UserName == registerDto.Username &&
                u.FirstName == registerDto.FirstName &&
                u.LastName == registerDto.LastName),
            registerDto.Password), Times.Once);
    }

    [Fact]
    public async Task UserSignUp_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "existing@mibo.com",
            Password = "Test123!",
            Username = "existinguser",
            FirstName = "Existing",
            LastName = "User"
        };

        var errors = new List<IdentityError>
        {
            new IdentityError { Description = "Email 'existing@mibo.com' is already taken." }
        };

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Failed(errors.ToArray()));

        // Act
        var result = await _sut.UserSignUp(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already taken");
    }

    [Fact]
    public async Task RefreshToken_WithInvalidUser_ReturnsEmptyResponse()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var tokenDto = new TokenDto
        {
            AccessToken = "invalid-access-token",
            RefreshToken = "invalid-refresh-token"
        };

        // The GetTokenPrincipal will return null for invalid token
        // which will cause the method to return empty LoginResponse

        // Act
        var result = await _sut.RefreshToken(tokenDto);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().BeNullOrEmpty();
        result.RefreshToken.Should().BeNullOrEmpty();
    }

}