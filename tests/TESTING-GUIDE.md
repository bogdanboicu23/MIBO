# MIBO Testing Guide — From Zero to 80% Coverage

## Table of Contents
1. [How Testing Works](#1-how-testing-works)
2. [Your Toolkit](#2-your-toolkit)
3. [How to Run Tests](#3-how-to-run-tests)
4. [How to Measure Coverage](#4-how-to-measure-coverage)
5. [Test File Organization](#5-test-file-organization)
6. [Complete Test List by Service](#6-complete-test-list-by-service)
7. [Full Code Examples — One Per Service](#7-full-code-examples)
8. [Common Patterns Cheat Sheet](#8-common-patterns-cheat-sheet)

---

## 1. How Testing Works

### The Testing Pyramid
```
         /\
        /E2E\         <-- 5%  - Full system running, browser/HTTP
       /------\
      / Integ  \      <-- 15% - Real DB (Testcontainers), multiple classes
     /----------\
    /   Unit     \    <-- 80% - One class at a time, mocked dependencies
   /--------------\
```

### What is a Unit Test?
A unit test verifies **one method** of **one class** in isolation.
Everything that class depends on (database, HTTP clients, other services) is replaced with a **mock** — a fake object you control.

### The AAA Pattern (every test follows this)
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // ARRANGE — set up the objects and mocks
    var mockDep = new Mock<IDependency>();
    mockDep.Setup(x => x.DoSomething()).Returns("value");
    var sut = new MyService(mockDep.Object); // sut = System Under Test

    // ACT — call the method you're testing
    var result = await sut.MyMethod();

    // ASSERT — verify the result is what you expect
    result.Should().Be("value");
}
```

### Key Concepts

| Concept | What it means | Example |
|---------|--------------|---------|
| `[Fact]` | Marks a method as a test | `[Fact] public void MyTest()` |
| `[Theory]` | Same test with different inputs | `[Theory] [InlineData("a")] [InlineData("b")]` |
| `Mock<T>` | Creates a fake implementation of interface T | `new Mock<IAuthService>()` |
| `.Setup()` | Tells the mock what to return when called | `.Setup(x => x.Login(...)).ReturnsAsync(result)` |
| `.Verify()` | Asserts the mock was called | `.Verify(x => x.Save(), Times.Once)` |
| `.Should()` | FluentAssertions — readable assertions | `result.Should().NotBeNull()` |

---

## 2. Your Toolkit

Already installed in every `.csproj`:

| Package | Purpose |
|---------|---------|
| **xUnit** | Test framework — `[Fact]`, `[Theory]`, test runner |
| **Moq** | Mocking framework — `Mock<IService>`, `.Setup()`, `.Verify()` |
| **FluentAssertions** | Readable assertions — `.Should().Be()`, `.Should().NotBeNull()` |
| **AutoFixture** | Auto-generates test data — random strings, objects |
| **coverlet.collector** | Measures code coverage |
| **Microsoft.AspNetCore.Mvc.Testing** | Testing ASP.NET controllers with `WebApplicationFactory` |
| **Testcontainers** | Spins up real PostgreSQL/MongoDB in Docker for integration tests |

---

## 3. How to Run Tests

```bash
# Run ALL tests
cd /Users/bogdanboicu/Desktop/Universitate/MASTER/MIBO/tests
dotnet test MIBO.Tests.sln

# Run tests for ONE project
dotnet test MIBO.IdentityService.Tests/

# Run tests matching a name filter
dotnet test --filter "AuthService"

# Run with verbose output (see each test name)
dotnet test --verbosity normal
```

---

## 4. How to Measure Coverage

```bash
# Generate coverage data
dotnet test MIBO.Tests.sln \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage-results

# Install the report tool (one time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator \
  -reports:"./coverage-results/**/coverage.cobertura.xml" \
  -targetdir:"./coverage-report" \
  -reporttypes:Html

# Open the report
open ./coverage-report/index.html
```

The report shows you **exactly which lines are covered** (green) and which are not (red).

---

## 5. Test File Organization

```
tests/
├── MIBO.IdentityService.Tests/
│   └── Unit/
│       ├── AuthServiceTests.cs          <-- AuthService logic
│       ├── TokenServiceTests.cs         <-- TokenService logic
│       ├── AuthControllerTests.cs       <-- Controller endpoints
│       └── TurnstileVerifierTests.cs    <-- Captcha verification
│
├── MIBO.ConversationService.Tests/
│   └── Unit/
│       ├── ChatControllerTests.cs       <-- CRUD + streaming
│       └── SseParsingTests.cs           <-- SSE helper methods
│
├── MIBO.ActionService.Tests/            <-- (create this project)
│   └── Unit/
│       ├── ActionsControllerTests.cs
│       └── ActionRouterTests.cs
│
├── MIBO.Storage.Mongo.Tests/            <-- (create this project)
│   └── Unit/
│       ├── MongoConversationStoreTests.cs
│       ├── MongoUiInstanceStoreTests.cs
│       └── MongoIndexHostedServiceTests.cs
│
├── MIBO.ApiGateway.Tests/
│   └── Unit/
│       └── GatewayRoutingTests.cs
│
├── MIBO.IntegrationTests/
│   ├── IdentityIntegrationTests.cs
│   └── ConversationIntegrationTests.cs
│
└── MIBO.Tests.sln
```

**Rule:** One test class per source class. Name it `{SourceClass}Tests.cs`.

---

## 6. Complete Test List by Service

### 6.1 IdentityService (42 tests) — HIGHEST PRIORITY

#### AuthServiceTests.cs (18 tests)

```
[Fact] UserLogin_ValidCredentials_ReturnsTokens
    Mock: UserManager.FindByEmailAsync → returns user
    Mock: UserManager.CheckPasswordAsync → returns true
    Mock: UserManager.GetRolesAsync → returns ["User"]
    Mock: UserManager.UpdateAsync → returns IdentityResult.Success
    Assert: result.AccessToken is not null/empty
    Assert: result.RefreshToken is not null/empty

[Fact] UserLogin_UserNotFound_ReturnsEmptyResponse
    Mock: UserManager.FindByEmailAsync → returns null
    Assert: result.AccessToken is null or empty

[Fact] UserLogin_WrongPassword_ReturnsEmptyResponse
    Mock: UserManager.FindByEmailAsync → returns user
    Mock: UserManager.CheckPasswordAsync → returns false
    Assert: result.AccessToken is null or empty

[Fact] UserSignUp_ValidData_ReturnsSuccess
    Mock: UserManager.CreateAsync → returns IdentityResult.Success
    Assert: result.Success == true

[Fact] UserSignUp_DuplicateEmail_ReturnsFailure
    Mock: UserManager.CreateAsync → returns IdentityResult.Failed(...)
    Assert: result.Success == false
    Assert: result.Message contains error description

[Fact] UserSignUp_SetsCorrectUserProperties
    Mock: UserManager.CreateAsync → capture the user parameter
    Assert: user.Email, UserName, FirstName, LastName match input

[Fact] RefreshToken_ValidTokens_ReturnsNewTokenPair
    Setup: Generate a real JWT with known claims
    Mock: UserManager.FindByIdAsync → returns user with matching RefreshToken
    Assert: result.AccessToken is not null
    Assert: result.RefreshToken is not null

[Fact] RefreshToken_InvalidAccessToken_ReturnsEmpty
    Input: accessToken = "not.a.valid.jwt"
    Assert: result.AccessToken is null

[Fact] RefreshToken_UserNotFound_ReturnsEmpty
    Mock: UserManager.FindByIdAsync → returns null
    Assert: result.AccessToken is null

[Fact] RefreshToken_MismatchedRefreshToken_ReturnsEmpty
    Mock: UserManager.FindByIdAsync → returns user with DIFFERENT RefreshToken
    Assert: result.AccessToken is null

[Fact] RefreshToken_ExpiredRefreshToken_ReturnsEmpty
    Setup: user.RefreshToken = a JWT that expired yesterday
    Assert: result.AccessToken is null

[Fact] GenerateLoginResponse_NoRoles_AddsDefaultUserRole
    Mock: UserManager.GetRolesAsync → returns empty list
    Act: call UserLogin with valid credentials
    Assert: the returned JWT contains a "role" claim with value "User"

[Fact] GenerateLoginResponse_WithRoles_IncludesAllRoleClaims
    Mock: UserManager.GetRolesAsync → returns ["Admin", "Manager"]
    Assert: JWT contains both role claims

[Fact] GenerateLoginResponse_ExistingValidRefreshToken_ReusesIt
    Setup: user.RefreshToken = a valid non-expired JWT
    Act: login
    Assert: result.RefreshToken == the existing one (not regenerated)

[Fact] GenerateLoginResponse_UpdatesLastLoginAt
    Act: login with valid credentials
    Verify: UserManager.UpdateAsync was called
    Assert: user.LastLoginAt is recent (within seconds of UtcNow)

[Fact] GenerateToken_ContainsExpectedClaims
    Act: login, decode the returned JWT
    Assert: has Sub, Email, Jti, Iat, NameIdentifier, Name, firstName, lastName

[Fact] IsJwtExpired_ExpiredToken_ReturnsTrue
    (Use reflection or make method internal + InternalsVisibleTo)

[Fact] IsJwtExpired_InvalidToken_ThrowsArgumentException
```

#### TokenServiceTests.cs (9 tests)

```
[Fact] GenerateAccessToken_ReturnsValidJwt
    Create a real ApplicationUser, call GenerateAccessToken
    Assert: result is not null/empty
    Assert: JwtSecurityTokenHandler.CanReadToken(result) == true

[Fact] GenerateAccessToken_ContainsUserIdClaim
    Decode the token, assert Sub claim == user.Id

[Fact] GenerateAccessToken_ContainsEmailClaim
    Decode the token, assert Email claim == user.Email

[Fact] GenerateAccessToken_ContainsUsernameClaim
    Decode, assert "username" claim == user.UserName

[Fact] GenerateAccessToken_IncludesFirstNameWhenPresent
    user.FirstName = "John" → token has "first_name" = "John"

[Fact] GenerateAccessToken_ExcludesFirstNameWhenNull
    user.FirstName = null → token does NOT have "first_name"

[Fact] GenerateAccessToken_SetsCorrectExpiration
    Decode, check exp claim matches config AccessTokenExpirationMinutes

[Fact] GenerateRefreshToken_ReturnsBase64String
    Assert: result is not null, is valid base64, length > 0

[Fact] GenerateRefreshToken_ReturnsDifferentValueEachCall
    var t1 = service.GenerateRefreshToken();
    var t2 = service.GenerateRefreshToken();
    t1.Should().NotBe(t2);

[Fact] ValidateToken_ValidToken_ReturnsPrincipal
    Generate a token, then validate it
    Assert: principal is not null, has expected claims

[Fact] ValidateToken_InvalidToken_ReturnsNull
    ValidateToken("garbage") → null

[Fact] ValidateToken_WrongSigningKey_ReturnsNull
    Generate token with one key, validate with different key → null
```

#### AuthControllerTests.cs (13 tests)

```
[Fact] Login_TurnstileEnabled_MissingToken_ReturnsBadRequest
    Config: Turnstile:Enabled = true
    Input: TurnstileToken = null
    Assert: BadRequest with "Captcha verification required"

[Fact] Login_TurnstileEnabled_FailedVerification_ReturnsUnauthorized
    Mock: HttpClient returns { success: false, error-codes: ["invalid-input"] }
    Assert: Unauthorized

[Fact] Login_TurnstileDisabled_SkipsVerification
    Config: Turnstile:Enabled = false
    Input: no TurnstileToken
    Assert: proceeds to login (no 400)

[Fact] Login_ValidCredentials_ReturnsOkWithJwt
    Mock: IAuthService.UserLogin → returns LoginResponse with tokens
    Assert: Ok with { jwtToken = "..." }

[Fact] Login_InvalidCredentials_ReturnsBadRequest
    Mock: IAuthService.UserLogin → returns empty LoginResponse
    Assert: BadRequest("Login Failed")

[Fact] Login_SetsRefreshTokenCookie
    Act: successful login
    Assert: Response has "refreshToken" cookie with HttpOnly, Secure, SameSite=None

[Fact] Register_ValidModel_ReturnsOk
    Mock: IAuthService.UserSignUp → returns ApiResponse.Ok()
    Assert: Ok with "User registered successfully"

[Fact] Register_InvalidModelState_ReturnsBadRequest
    Add model error to ModelState
    Assert: BadRequest

[Fact] Register_ServiceFails_ReturnsBadRequest
    Mock: IAuthService.UserSignUp → returns ApiResponse.Fail("Email taken")
    Assert: BadRequest with message

[Fact] RefreshToken_MissingCookie_ReturnsUnauthorized
    No "refreshToken" cookie in request
    Assert: Unauthorized

[Fact] RefreshToken_Valid_ReturnsNewJwt
    Set cookie + valid JwtToken
    Mock: IAuthService.RefreshToken → returns tokens
    Assert: Ok with { jwtToken }

[Fact] Test_ReturnsServiceInfo
    GET /api/auth/test
    Assert: Ok with message, timestamp, version

[Fact] GetCurrentUser_ReturnsUserFromClaims
    Set ClaimsPrincipal with known claims
    Assert: response contains correct user info
```

#### TurnstileVerifierTests.cs (5 tests)

```
[Fact] VerifyAsync_Success_ReturnsTrue
    Mock HttpClient (via MockHttpMessageHandler) to return { success: true }
    Assert: ok == true, errors is empty

[Fact] VerifyAsync_Failure_ReturnsFalseWithErrors
    Return { success: false, error-codes: ["invalid-input-response"] }
    Assert: ok == false, errors contains "invalid-input-response"

[Fact] VerifyAsync_HttpError_ReturnsFalse
    Return HTTP 500
    Assert: ok == false

[Fact] VerifyAsync_Timeout_ReturnsFalse
    Throw TaskCanceledException
    Assert: ok == false, errors contains "Request timeout"

[Fact] VerifyAsync_NetworkError_ReturnsFalse
    Throw HttpRequestException
    Assert: ok == false
```

---

### 6.2 ConversationService (20 tests) — HIGH PRIORITY

#### ChatControllerTests.cs (14 tests)

```
[Fact] ListConversations_ReturnsOkWithList
[Fact] ListConversations_UsesXUserIdHeader
[Fact] ListConversations_DefaultsToAnonymous
[Fact] CreateConversation_ReturnsOkWithSummary
[Fact] CreateConversation_UsesBodyUserId
[Fact] GetConversation_Found_ReturnsOk
[Fact] GetConversation_NotFound_Returns404
[Fact] RenameConversation_ValidTitle_ReturnsOk
[Fact] RenameConversation_EmptyTitle_ReturnsBadRequest
[Fact] RenameConversation_NotFound_Returns404
[Fact] DeleteConversation_Found_ReturnsOk
[Fact] DeleteConversation_NotFound_Returns404
[Fact] ResolveUserId_Priority_ExplicitFirst_ThenHeader_ThenQuery_ThenAnonymous
[Fact] PostChat_EmptyMessage_Returns400
```

#### SseParsingTests.cs (6 tests)

```
[Fact] TryReadSseEnvelope_ValidDoneEvent_ExtractsTypeAndContent
[Fact] TryReadSseEnvelope_NoDataPrefix_ReturnsFalse
[Fact] TryReadSseEnvelope_InvalidJson_ReturnsFalse
[Fact] TryReadSseEnvelope_NoTypeProperty_ReturnsFalse
[Fact] TryParseAssistantPayload_ValidJsonObject_ReturnsPayloadAndText
[Fact] TryParseAssistantPayload_PlainString_ReturnsFalse
[Fact] TryParseAssistantPayload_EmptyString_ReturnsFalse
```

**Note:** `TryReadSseEnvelope` and `TryParseAssistantPayload` are `private static`. To test them:
- Option A: Change to `internal static` and add `[InternalsVisibleTo("MIBO.ConversationService.Tests")]` in the source project
- Option B: Test them indirectly through controller integration tests

---

### 6.3 ActionService (16 tests) — MEDIUM PRIORITY

#### ActionsControllerTests.cs (4 tests)

```
[Fact] Query_NullDataSource_ReturnsBadRequest
[Fact] Query_ValidRequest_ReturnsQueryResponse
[Fact] Execute_NullAction_ReturnsBadRequest
[Fact] Execute_ValidRequest_ReturnsExecutionResponse
```

#### ActionRouterTests.cs (12 tests)

```
[Fact] QueryAsync_MergesDefaultArgsWithRequestArgs
[Fact] QueryAsync_UsesIdWhenHandlerIsEmpty
[Fact] QueryAsync_AppliesTransforms
[Fact] QueryAsync_InfersFieldHints
[Fact] ExecuteAsync_MergesPayloadWithDefaults
[Fact] ExecuteAsync_RefreshesSpecifiedDataSources
[Fact] MergeArguments_RequestOverridesDefaults
[Fact] MergeArguments_BothNull_ReturnsEmptyDict
[Fact] MergeArguments_OneNull_ReturnsOther
[Fact] ApplyTransforms_NullTransforms_ReturnsOriginalData
[Fact] ApplyTransforms_SelectTransform_FiltersFields
[Fact] InferFieldHints_KnownHandler_ReturnsHints
```

---

### 6.4 Storage.Mongo (13 tests) — MEDIUM PRIORITY

You need to create `MIBO.Storage.Mongo.Tests` project or add tests to an existing one.

#### MongoConversationStoreTests.cs

```
[Fact] CreateConversationAsync_InsertsDoc_ReturnsSummary
[Fact] ListConversationsAsync_FiltersbyUserId
[Fact] ListConversationsAsync_AppliesSkipAndLimit
[Fact] GetConversationAsync_Found_ReturnsDetails
[Fact] GetConversationAsync_WrongUser_ReturnsNull
[Fact] RenameConversationAsync_UpdatesTitle
[Fact] RenameConversationAsync_NotFound_ReturnsFalse
[Fact] DeleteConversationAsync_RemovesDoc
[Fact] AppendUserMessageAsync_InsertsMessageDoc
[Fact] AppendUserMessageAsync_WrongUser_ThrowsOwnershipException
[Fact] AppendAssistantMessageAsync_WithUiPayload_StoresIt
```

#### ConversationRepositoryTests.cs

```
[Fact] AppendMessageAsync_CallsInsertOne
```

#### MongoIndexHostedServiceTests.cs

```
[Fact] EnsureIndexAsync_CreatesIndex
[Fact] EnsureIndexAsync_ConflictingDefinition_DropsAndRecreates
```

---

### 6.5 Cache.Redis (3 tests) — LOW PRIORITY

```
[Fact] GetAsync_KeyExists_ReturnsValue
[Fact] GetAsync_KeyMissing_ReturnsNull
[Fact] SetAsync_CallsStringSetWithTtl
```

---

### 6.6 Model / DTO Tests (5 tests) — LOW PRIORITY BUT EASY WINS

```
[Fact] ApiResponse_Ok_ReturnsTrueSuccess
[Fact] ApiResponse_Fail_ReturnsFalseWithMessage
[Fact] LoginResponse_DefaultValues_AreNull
[Fact] RegisterDto_RequiredFieldsValidation
[Fact] UserDto_RequiredFieldsValidation
```

---

## 7. Full Code Examples

### Example 1: AuthServiceTests.cs (complete starter file)

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using MIBO.IdentityService.Data;
using MIBO.IdentityService.Models;
using MIBO.IdentityService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace MIBO.IdentityService.Tests.Unit;

public class AuthServiceTests
{
    // ---------- shared setup ----------
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly JwtSettingsOptions _jwtSettings;
    private readonly AuthService _sut; // System Under Test

    public AuthServiceTests()
    {
        // UserManager requires a mock IUserStore to instantiate
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _jwtSettings = new JwtSettingsOptions
        {
            AccessTokenSecret = "ThisIsASecretKeyForTestingThatIs256BitsLong!!",
            RefreshTokenSecret = "ThisIsARefreshSecretKeyForTestingPurposes!!!",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        _sut = new AuthService(
            _userManagerMock.Object,
            Options.Create(_jwtSettings));
    }

    // ---------- helper ----------
    private ApplicationUser CreateTestUser(string id = "user-123") => new()
    {
        Id = id,
        Email = "test@mibo.com",
        UserName = "testuser",
        FirstName = "Test",
        LastName = "User"
    };

    // ================================================================
    // UserLogin tests
    // ================================================================

    [Fact]
    public async Task UserLogin_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        var user = CreateTestUser();
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@mibo.com"))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "Pass123!"))
            .ReturnsAsync(true);
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });
        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var dto = new UserDto { Email = "test@mibo.com", Password = "Pass123!" };

        // Act
        var result = await _sut.UserLogin(dto);

        // Assert
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UserLogin_UserNotFound_ReturnsEmptyResponse()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var dto = new UserDto { Email = "nobody@test.com", Password = "pass" };

        // Act
        var result = await _sut.UserLogin(dto);

        // Assert
        result.AccessToken.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task UserLogin_WrongPassword_ReturnsEmptyResponse()
    {
        // Arrange
        var user = CreateTestUser();
        _userManagerMock.Setup(x => x.FindByEmailAsync("test@mibo.com"))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>()))
            .ReturnsAsync(false); // <-- wrong password

        var dto = new UserDto { Email = "test@mibo.com", Password = "wrong" };

        // Act
        var result = await _sut.UserLogin(dto);

        // Assert
        result.AccessToken.Should().BeNullOrEmpty();
    }

    // ================================================================
    // UserSignUp tests
    // ================================================================

    [Fact]
    public async Task UserSignUp_ValidData_ReturnsSuccess()
    {
        // Arrange
        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var dto = new RegisterDto
        {
            Email = "new@mibo.com",
            Password = "Strong123!",
            Username = "newuser",
            FirstName = "New",
            LastName = "User"
        };

        // Act
        var result = await _sut.UserSignUp(dto);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UserSignUp_IdentityFails_ReturnsFailWithMessage()
    {
        // Arrange
        var errors = new[] { new IdentityError { Description = "Email already taken" } };
        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(errors));

        var dto = new RegisterDto
        {
            Email = "dup@mibo.com",
            Password = "Pass123!",
            Username = "dup"
        };

        // Act
        var result = await _sut.UserSignUp(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Email already taken");
    }

    // ================================================================
    // RefreshToken tests
    // ================================================================

    [Fact]
    public async Task RefreshToken_InvalidAccessToken_ReturnsEmpty()
    {
        // Arrange — "not.valid" can't be decoded
        var dto = new TokenDto
        {
            AccessToken = "not.a.valid.token",
            RefreshToken = "doesnt-matter"
        };

        // Act
        var result = await _sut.RefreshToken(dto);

        // Assert
        result.AccessToken.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshToken_UserNotFound_ReturnsEmpty()
    {
        // Arrange — generate a real JWT so GetTokenPrincipal succeeds
        var realJwt = GenerateTestJwt("user-999");
        _userManagerMock.Setup(x => x.FindByIdAsync("user-999"))
            .ReturnsAsync((ApplicationUser?)null);

        var dto = new TokenDto { AccessToken = realJwt, RefreshToken = "abc" };

        // Act
        var result = await _sut.RefreshToken(dto);

        // Assert
        result.AccessToken.Should().BeNullOrEmpty();
    }

    // ================================================================
    // GenerateLoginResponse — role claims
    // ================================================================

    [Fact]
    public async Task UserLogin_NoRoles_JwtContainsDefaultUserRole()
    {
        // Arrange
        var user = CreateTestUser();
        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "p")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>()); // empty
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.UserLogin(new UserDto { Email = user.Email!, Password = "p" });

        // Assert — decode the JWT and check role claim
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.AccessToken);
        var roles = token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        roles.Should().Contain("User");
    }

    // ---------- helper to generate JWTs for testing ----------
    private string GenerateTestJwt(string userId)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.AccessTokenSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(JwtRegisteredClaimNames.Sub, userId)
        };
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Example 2: TokenServiceTests.cs

```csharp
using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using MIBO.IdentityService.Data;
using MIBO.IdentityService.Models;
using MIBO.IdentityService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MIBO.IdentityService.Tests.Unit;

public class TokenServiceTests
{
    private readonly TokenService _sut;
    private readonly JwtSettingsOptions _settings;

    public TokenServiceTests()
    {
        _settings = new JwtSettingsOptions
        {
            AccessTokenSecret = "ThisIsASecretKeyForTestingThatIs256BitsLong!!",
            RefreshTokenSecret = "ThisIsARefreshSecretKeyForTestingPurposes!!!",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        };

        _sut = new TokenService(
            Options.Create(_settings),
            Mock.Of<ILogger<TokenService>>());
    }

    private ApplicationUser CreateUser() => new()
    {
        Id = "u1",
        Email = "a@b.com",
        UserName = "auser",
        FirstName = "Alice",
        LastName = "Bob"
    };

    [Fact]
    public void GenerateAccessToken_ReturnsReadableJwt()
    {
        var token = _sut.GenerateAccessToken(CreateUser());

        token.Should().NotBeNullOrEmpty();
        new JwtSecurityTokenHandler().CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_ContainsSubClaim()
    {
        var token = _sut.GenerateAccessToken(CreateUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Subject.Should().Be("u1");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64()
    {
        var token = _sut.GenerateRefreshToken();

        token.Should().NotBeNullOrEmpty();
        var act = () => Convert.FromBase64String(token);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateRefreshToken_IsUnique()
    {
        var t1 = _sut.GenerateRefreshToken();
        var t2 = _sut.GenerateRefreshToken();

        t1.Should().NotBe(t2);
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsPrincipal()
    {
        var token = _sut.GenerateAccessToken(CreateUser());
        var principal = _sut.ValidateToken(token);

        principal.Should().NotBeNull();
    }

    [Fact]
    public void ValidateToken_GarbageToken_ReturnsNull()
    {
        var principal = _sut.ValidateToken("not.a.jwt.at.all");

        principal.Should().BeNull();
    }
}
```

### Example 3: ChatControllerTests.cs (ConversationService)

```csharp
using FluentAssertions;
using MIBO.ConversationService.Controllers;
using MIBO.Storage.Mongo.Store.Conversation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace MIBO.ConversationService.Tests.Unit;

public class ChatControllerTests
{
    private readonly Mock<IConversationStore> _storeMock;
    private readonly Mock<IHttpClientFactory> _httpFactoryMock;
    private readonly ChatController _sut;

    public ChatControllerTests()
    {
        _storeMock = new Mock<IConversationStore>();
        _httpFactoryMock = new Mock<IHttpClientFactory>();

        _sut = new ChatController(
            _httpFactoryMock.Object,
            _storeMock.Object,
            Mock.Of<ILogger<ChatController>>());

        // Set up a default HttpContext so Request/Response work
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task ListConversations_ReturnsOkWithList()
    {
        // Arrange
        var conversations = new List<ConversationSummary>
        {
            new() { ConversationId = "c1", Title = "Chat 1" }
        };
        _storeMock
            .Setup(x => x.ListConversationsAsync("anonymous", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversations);

        // Act
        var result = await _sut.ListConversations();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(conversations);
    }

    [Fact]
    public async Task ListConversations_UsesXUserIdHeader()
    {
        // Arrange
        _sut.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = "user-42";
        _storeMock
            .Setup(x => x.ListConversationsAsync("user-42", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversationSummary>());

        // Act
        await _sut.ListConversations();

        // Assert — verify the store was called with the header userId
        _storeMock.Verify(
            x => x.ListConversationsAsync("user-42", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConversation_NotFound_Returns404()
    {
        // Arrange
        _storeMock
            .Setup(x => x.GetConversationAsync("c99", "anonymous", 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationDetails?)null);

        // Act
        var result = await _sut.GetConversation("c99");

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RenameConversation_EmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var request = new RenameConversationRequest { Title = "  " };

        // Act
        var result = await _sut.RenameConversation("c1", request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteConversation_Found_ReturnsOk()
    {
        // Arrange
        _storeMock
            .Setup(x => x.DeleteConversationAsync("c1", "anonymous", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteConversation("c1", null, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
```

### Example 4: ActionsControllerTests.cs

```csharp
using FluentAssertions;
using MIBO.ActionService.Controllers;
using MIBO.ActionService.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace MIBO.ActionService.Tests.Unit;

public class ActionsControllerTests
{
    private readonly Mock<IActionRouter> _routerMock = new();
    private readonly ActionsController _sut;

    public ActionsControllerTests()
    {
        _sut = new ActionsController(_routerMock.Object);
    }

    [Fact]
    public async Task Query_NullDataSource_ReturnsBadRequest()
    {
        var request = new QueryRequest { DataSource = null };

        var result = await _sut.Query(request, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Query_ValidRequest_ReturnsOk()
    {
        var dataSource = new DataSourceDefinition { Id = "ds1", Handler = "products.list" };
        var expectedResponse = new QueryResponse { DataSourceId = "ds1" };

        _routerMock
            .Setup(x => x.QueryAsync(dataSource, It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var request = new QueryRequest { DataSource = dataSource };

        var result = await _sut.Query(request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Execute_NullAction_ReturnsBadRequest()
    {
        var request = new ExecuteActionRequest { Action = null };

        var result = await _sut.Execute(request, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
```

### Example 5: TurnstileVerifierTests.cs (mocking HttpClient)

```csharp
using System.Net;
using System.Text;
using FluentAssertions;
using MIBO.IdentityService.Models;

namespace MIBO.IdentityService.Tests.Unit;

public class TurnstileVerifierTests
{
    // This is the pattern for mocking HttpClient
    private static HttpClient CreateMockClient(HttpStatusCode status, string jsonBody)
    {
        var handler = new MockHttpMessageHandler(status, jsonBody);
        return new HttpClient(handler);
    }

    [Fact]
    public async Task VerifyAsync_SuccessResponse_ReturnsTrue()
    {
        var client = CreateMockClient(HttpStatusCode.OK,
            """{"success": true, "error-codes": []}""");

        var (ok, errors) = await TurnstileVerifier.VerifyAsync(
            "token", "secret", "127.0.0.1", client, CancellationToken.None);

        ok.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifyAsync_FailureResponse_ReturnsFalseWithErrors()
    {
        var client = CreateMockClient(HttpStatusCode.OK,
            """{"success": false, "error-codes": ["invalid-input-response"]}""");

        var (ok, errors) = await TurnstileVerifier.VerifyAsync(
            "bad-token", "secret", null, client, CancellationToken.None);

        ok.Should().BeFalse();
        errors.Should().Contain("invalid-input-response");
    }

    [Fact]
    public async Task VerifyAsync_HttpError_ReturnsFalse()
    {
        var client = CreateMockClient(HttpStatusCode.InternalServerError, "");

        var (ok, _) = await TurnstileVerifier.VerifyAsync(
            "token", "secret", null, client, CancellationToken.None);

        ok.Should().BeFalse();
    }
}

/// <summary>
/// A reusable mock for HttpClient — put this in a shared Helpers/ folder.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;

    public MockHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
```

### Example 6: Model Tests (easy wins)

```csharp
using FluentAssertions;
using MIBO.IdentityService.Models;

namespace MIBO.IdentityService.Tests.Unit;

public class AuthModelsTests
{
    [Fact]
    public void ApiResponse_Ok_HasSuccessTrue()
    {
        var response = ApiResponse.Ok();

        response.Success.Should().BeTrue();
        response.Message.Should().BeNull();
    }

    [Fact]
    public void ApiResponse_Fail_HasSuccessFalseAndMessage()
    {
        var response = ApiResponse.Fail("Something went wrong");

        response.Success.Should().BeFalse();
        response.Message.Should().Be("Something went wrong");
    }

    [Fact]
    public void LoginResponse_DefaultsToNull()
    {
        var response = new LoginResponse();

        response.AccessToken.Should().BeNull();
        response.RefreshToken.Should().BeNull();
    }
}
```

---

## 8. Common Patterns Cheat Sheet

### Mocking UserManager (required for Identity tests)
```csharp
var store = new Mock<IUserStore<ApplicationUser>>();
var userManager = new Mock<UserManager<ApplicationUser>>(
    store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
```

### Mocking IOptions<T>
```csharp
var options = Options.Create(new JwtSettingsOptions { ... });
```

### Mocking IHttpClientFactory
```csharp
var factory = new Mock<IHttpClientFactory>();
factory.Setup(x => x.CreateClient("agent")).Returns(httpClient);
```

### Mocking ILogger<T> (just use Mock.Of)
```csharp
Mock.Of<ILogger<MyService>>()
```

### Setting up HttpContext for controller tests
```csharp
controller.ControllerContext = new ControllerContext
{
    HttpContext = new DefaultHttpContext()
};
// Add headers:
controller.ControllerContext.HttpContext.Request.Headers["X-User-Id"] = "u1";
// Add cookies:
controller.ControllerContext.HttpContext.Request.Headers["Cookie"] = "refreshToken=abc";
```

### Verifying a mock was called
```csharp
_mockService.Verify(x => x.SaveAsync(It.IsAny<string>()), Times.Once);
_mockService.Verify(x => x.DeleteAsync("id-123"), Times.Never);
```

### Testing exceptions
```csharp
var act = async () => await _sut.DoSomething();
await act.Should().ThrowAsync<InvalidOperationException>()
    .WithMessage("*expected part*");
```

### [Theory] with multiple inputs
```csharp
[Theory]
[InlineData("", false)]
[InlineData("  ", false)]
[InlineData("valid@email.com", true)]
public void IsValidEmail_ReturnsExpected(string email, bool expected)
{
    var result = Validator.IsValidEmail(email);
    result.Should().Be(expected);
}
```

### Making private/internal methods testable
Add this to the **source project's .csproj** (not the test project):
```xml
<ItemGroup>
  <InternalsVisibleTo Include="MIBO.ConversationService.Tests" />
</ItemGroup>
```
Then change `private static` to `internal static` on methods like `TryReadSseEnvelope`.

---

## Recommended Order of Implementation

Start with the easiest, highest-impact tests first:

| Step | What | Tests | Why first |
|------|------|-------|-----------|
| 1 | `AuthModelsTests` | 3 | Trivial, builds confidence |
| 2 | `TokenServiceTests` | 6 | Pure logic, no complex mocks |
| 3 | `TurnstileVerifierTests` | 3-5 | Static method, easy to test |
| 4 | `AuthServiceTests` | 10-18 | Core business logic, biggest coverage gain |
| 5 | `AuthControllerTests` | 10-13 | Controller layer on top of mocked service |
| 6 | `ChatControllerTests` | 14 | Second service, same patterns |
| 7 | `ActionsControllerTests` | 4 | Simple request validation |
| 8 | `ActionRouterTests` | 12 | Complex routing logic |
| 9 | `MongoStoreTests` | 11-13 | Data layer |
| 10 | `RedisToolCacheTests` | 3 | Simple cache layer |

After steps 1-6 you'll already be near 60-70% coverage. Steps 7-10 push you past 80%.
