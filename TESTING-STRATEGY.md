# MIBO Comprehensive Testing Strategy

## Current State Analysis
❌ **No existing test projects found** - Critical gap in quality assurance

## Testing Pyramid for MIBO

```
        /\
       /E2E\      <- 10% - End-to-End Tests
      /------\
     /  Integ \   <- 20% - Integration Tests
    /----------\
   / Component  \ <- 30% - Component Tests
  /--------------\
 /   Unit Tests   \ <- 40% - Unit Tests
/------------------\
```

## 1. Unit Tests (40% of tests)

### A. IdentityService Unit Tests

#### Test Project Setup
```bash
dotnet new xunit -n MIBO.IdentityService.Tests
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package AutoFixture
```

#### Example Tests to Implement

**1. Authentication Tests**
```csharp
[Fact]
public async Task Login_WithValidCredentials_ReturnsJwtToken()
{
    // Arrange
    var userManager = new Mock<UserManager<ApplicationUser>>();
    var user = new ApplicationUser { Email = "test@mibo.com" };
    userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
        .ReturnsAsync(user);
    userManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>()))
        .ReturnsAsync(true);

    var service = new AuthenticationService(userManager.Object, _tokenService);

    // Act
    var result = await service.LoginAsync("test@mibo.com", "password123");

    // Assert
    result.Should().NotBeNull();
    result.Token.Should().NotBeEmpty();
    result.RefreshToken.Should().NotBeEmpty();
}

[Fact]
public async Task Login_WithInvalidCredentials_ThrowsUnauthorizedException()
{
    // Arrange
    var userManager = new Mock<UserManager<ApplicationUser>>();
    userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
        .ReturnsAsync((ApplicationUser)null);

    var service = new AuthenticationService(userManager.Object, _tokenService);

    // Act & Assert
    await service.Invoking(s => s.LoginAsync("invalid@mibo.com", "wrong"))
        .Should().ThrowAsync<UnauthorizedException>();
}
```

**2. JWT Token Generation Tests**
```csharp
[Fact]
public void GenerateJwtToken_CreatesValidToken_WithCorrectClaims()
{
    // Arrange
    var tokenService = new JwtTokenService(_configuration);
    var userId = Guid.NewGuid().ToString();
    var email = "user@mibo.com";

    // Act
    var token = tokenService.GenerateToken(userId, email, new[] { "User" });
    var handler = new JwtSecurityTokenHandler();
    var jsonToken = handler.ReadJwtToken(token);

    // Assert
    jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId);
    jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == email);
    jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "User");
}

[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData(" ")]
public void GenerateJwtToken_WithInvalidUserId_ThrowsArgumentException(string userId)
{
    // Arrange
    var tokenService = new JwtTokenService(_configuration);

    // Act & Assert
    tokenService.Invoking(s => s.GenerateToken(userId, "email@test.com", new[] { "User" }))
        .Should().Throw<ArgumentException>();
}
```

### B. ConversationService Unit Tests

**1. Message Processing Tests**
```csharp
[Fact]
public async Task ProcessMessage_WithValidInput_ReturnsAiResponse()
{
    // Arrange
    var mockLangChain = new Mock<ILangChainService>();
    var mockMessageRepo = new Mock<IMessageRepository>();

    mockLangChain.Setup(x => x.ProcessAsync(It.IsAny<string>()))
        .ReturnsAsync(new AiResponse { Content = "AI response" });

    var service = new ConversationService(mockLangChain.Object, mockMessageRepo.Object);

    // Act
    var result = await service.ProcessMessageAsync("user input", 1);

    // Assert
    result.Should().NotBeNull();
    result.Content.Should().Be("AI response");
    mockMessageRepo.Verify(x => x.SaveAsync(It.IsAny<Message>()), Times.Once);
}

[Fact]
public async Task ProcessMessage_WhenAiServiceFails_ReturnsErrorMessage()
{
    // Arrange
    var mockLangChain = new Mock<ILangChainService>();
    mockLangChain.Setup(x => x.ProcessAsync(It.IsAny<string>()))
        .ThrowsAsync(new ExternalServiceException("AI service unavailable"));

    var service = new ConversationService(mockLangChain.Object, _mockMessageRepo.Object);

    // Act
    var result = await service.ProcessMessageAsync("user input", 1);

    // Assert
    result.Should().NotBeNull();
    result.IsError.Should().BeTrue();
    result.ErrorMessage.Should().Contain("temporarily unavailable");
}
```

**2. Tool Execution Tests**
```csharp
[Fact]
public async Task ExecuteTool_WithValidToolRequest_ReturnsToolResult()
{
    // Arrange
    var mockToolExecutor = new Mock<IToolExecutor>();
    var toolRequest = new ToolRequest
    {
        ToolName = "GetWeather",
        Parameters = new { city = "London" }
    };

    mockToolExecutor.Setup(x => x.ExecuteAsync(It.IsAny<ToolRequest>()))
        .ReturnsAsync(new ToolResult { Success = true, Data = "20°C, Sunny" });

    var service = new ToolService(mockToolExecutor.Object);

    // Act
    var result = await service.ExecuteToolAsync(toolRequest);

    // Assert
    result.Success.Should().BeTrue();
    result.Data.Should().Be("20°C, Sunny");
}

[Fact]
public async Task ExecuteTool_WithTimeout_ThrowsTimeoutException()
{
    // Arrange
    var mockToolExecutor = new Mock<IToolExecutor>();
    mockToolExecutor.Setup(x => x.ExecuteAsync(It.IsAny<ToolRequest>()))
        .Returns(async () =>
        {
            await Task.Delay(6000); // Longer than timeout
            return new ToolResult();
        });

    var service = new ToolService(mockToolExecutor.Object, timeout: TimeSpan.FromSeconds(5));

    // Act & Assert
    await service.Invoking(s => s.ExecuteToolAsync(new ToolRequest()))
        .Should().ThrowAsync<TimeoutException>();
}
```

### C. FinanceDataService Unit Tests

**1. Financial Calculation Tests**
```csharp
[Fact]
public void CalculateExpensesByCategory_GroupsCorrectly()
{
    // Arrange
    var expenses = new List<Expense>
    {
        new() { Category = "food", Amount = 100 },
        new() { Category = "food", Amount = 50 },
        new() { Category = "transport", Amount = 75 }
    };

    var service = new FinancialService(_mockDataService.Object);

    // Act
    var result = service.GroupExpensesByCategory(expenses);

    // Assert
    result.Should().HaveCount(2);
    result["food"].Should().Be(150);
    result["transport"].Should().Be(75);
}

[Theory]
[InlineData("lastmonth", -1)]
[InlineData("last30days", -30)]
[InlineData("thisyear", 0)]
public void GetDateRangeForPeriod_ReturnsCorrectDates(string period, int expectedDaysAgo)
{
    // Arrange
    var service = new FinancialService(_mockDataService.Object);
    var now = DateTime.Now;

    // Act
    var (startDate, endDate) = service.GetDateRangeForPeriod(period);

    // Assert
    if (period == "lastmonth")
    {
        startDate.Month.Should().Be(now.AddMonths(-1).Month);
        endDate.Month.Should().Be(now.AddMonths(-1).Month);
    }
    else if (period == "last30days")
    {
        (now - startDate).Days.Should().BeCloseTo(30, 1);
    }
}
```

**2. Pagination Tests**
```csharp
[Theory]
[InlineData(100, 0, 10, 10)]
[InlineData(100, 20, 10, 10)]
[InlineData(100, 95, 10, 5)]
[InlineData(100, 100, 10, 0)]
public void GetPaginatedResults_ReturnsCorrectSubset(int totalItems, int skip, int limit, int expectedCount)
{
    // Arrange
    var items = Enumerable.Range(1, totalItems).ToList();

    // Act
    var result = PaginationHelper.Paginate(items, skip, limit);

    // Assert
    result.Items.Should().HaveCount(expectedCount);
    result.Total.Should().Be(totalItems);
    result.Skip.Should().Be(skip);
    result.Limit.Should().Be(limit);
}
```

## 2. Integration Tests (20% of tests)

### A. API Integration Tests

**Setup Test Project**
```bash
dotnet new xunit -n MIBO.IntegrationTests
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers
dotnet add package WireMock.Net
```

**1. API Gateway Integration Tests**
```csharp
public class ApiGatewayIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiGatewayIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAccount_WithValidToken_ReturnsAccountData()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/accounts/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var account = JsonSerializer.Deserialize<Account>(content);
        account.Should().NotBeNull();
        account.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetAccount_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/accounts/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RateLimiting_ExceedsLimit_Returns429()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act - Send 101 requests (assuming limit is 100)
        var tasks = Enumerable.Range(1, 101)
            .Select(_ => _client.GetAsync("/api/tools/execute"))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .Should().BeGreaterThan(0);
    }
}
```

**2. Database Integration Tests with Testcontainers**
```csharp
public class DatabaseIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgres;
    private MongoDbContainer _mongodb;
    private RedisContainer _redis;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("mibo_test")
            .Build();

        _mongodb = new MongoDbBuilder()
            .WithImage("mongo:7")
            .Build();

        _redis = new RedisBuilder()
            .WithImage("redis:7")
            .Build();

        await Task.WhenAll(
            _postgres.StartAsync(),
            _mongodb.StartAsync(),
            _redis.StartAsync()
        );
    }

    [Fact]
    public async Task SaveAndRetrieveUser_PostgreSQL_WorksCorrectly()
    {
        // Arrange
        var connectionString = _postgres.GetConnectionString();
        using var context = new ApplicationDbContext(connectionString);
        await context.Database.MigrateAsync();

        var user = new ApplicationUser
        {
            Email = "test@mibo.com",
            UserName = "testuser"
        };

        // Act
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var retrievedUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email == "test@mibo.com");

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser.UserName.Should().Be("testuser");
    }

    [Fact]
    public async Task SaveAndRetrieveConversation_MongoDB_WorksCorrectly()
    {
        // Arrange
        var connectionString = _mongodb.GetConnectionString();
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("mibo_test");
        var collection = database.GetCollection<Conversation>("conversations");

        var conversation = new Conversation
        {
            UserId = 1,
            Messages = new List<Message>
            {
                new() { Content = "Hello", Role = "user" },
                new() { Content = "Hi there!", Role = "assistant" }
            }
        };

        // Act
        await collection.InsertOneAsync(conversation);
        var retrieved = await collection.Find(c => c.UserId == 1).FirstOrDefaultAsync();

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task CacheAndRetrieve_Redis_WorksCorrectly()
    {
        // Arrange
        var connectionString = _redis.GetConnectionString();
        var redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var cache = redis.GetDatabase();

        // Act
        await cache.StringSetAsync("test:key", "test value", TimeSpan.FromMinutes(1));
        var value = await cache.StringGetAsync("test:key");

        // Assert
        value.HasValue.Should().BeTrue();
        value.ToString().Should().Be("test value");
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            _postgres.DisposeAsync().AsTask(),
            _mongodb.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask()
        );
    }
}
```

**3. Message Queue Integration Tests**
```csharp
public class NatsIntegrationTests : IAsyncLifetime
{
    private NatsContainer _nats;
    private IConnection _connection;

    public async Task InitializeAsync()
    {
        _nats = new NatsBuilder()
            .WithImage("nats:2.10-alpine")
            .WithCommand("-js") // Enable JetStream
            .Build();

        await _nats.StartAsync();
        _connection = new ConnectionFactory().CreateConnection(_nats.GetConnectionString());
    }

    [Fact]
    public async Task PublishAndSubscribe_Message_IsReceived()
    {
        // Arrange
        var received = new TaskCompletionSource<string>();
        var js = _connection.CreateJetStreamContext();

        await js.AddStreamAsync(new StreamConfig("TEST", new[] { "test.>" }));

        var subscription = await js.PushSubscribeAsync(
            "test.message",
            (sender, args) =>
            {
                var message = Encoding.UTF8.GetString(args.Message.Data);
                received.SetResult(message);
            }
        );

        // Act
        await js.PublishAsync("test.message", Encoding.UTF8.GetBytes("Hello NATS"));

        // Assert
        var result = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        result.Should().Be("Hello NATS");
    }

    public async Task DisposeAsync()
    {
        _connection?.Dispose();
        await _nats.DisposeAsync();
    }
}
```

## 3. Component Tests (30% of tests)

### A. SignalR Real-time Communication Tests

```csharp
public class SignalRComponentTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task SignalRHub_SendMessage_BroadcastsToClients()
    {
        // Arrange
        var factory = new WebApplicationFactory<Program>();
        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"{factory.Server.BaseAddress}conversationHub", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();

        var receivedMessage = new TaskCompletionSource<string>();

        hubConnection.On<string>("ReceiveMessage", message =>
        {
            receivedMessage.SetResult(message);
        });

        await hubConnection.StartAsync();

        // Act
        await hubConnection.InvokeAsync("SendMessage", "Test message");

        // Assert
        var result = await receivedMessage.Task.WaitAsync(TimeSpan.FromSeconds(5));
        result.Should().Be("Test message");

        await hubConnection.DisposeAsync();
    }
}
```

### B. LangChain Service Component Tests

```csharp
public class LangChainComponentTests
{
    [Fact]
    public async Task LangChainPlanner_WithComplexQuery_GeneratesCorrectPlan()
    {
        // Arrange
        var mockGroqClient = new Mock<IGroqClient>();
        mockGroqClient.Setup(x => x.CompleteAsync(It.IsAny<string>()))
            .ReturnsAsync(@"{
                ""plan"": [
                    {""tool"": ""GetWeather"", ""params"": {""city"": ""London""}},
                    {""tool"": ""GetFinancialSummary"", ""params"": {""userId"": 1}}
                ]
            }");

        var planner = new LangChainPlanner(mockGroqClient.Object);

        // Act
        var plan = await planner.CreatePlanAsync(
            "What's the weather in London and show my financial summary"
        );

        // Assert
        plan.Steps.Should().HaveCount(2);
        plan.Steps[0].Tool.Should().Be("GetWeather");
        plan.Steps[1].Tool.Should().Be("GetFinancialSummary");
    }

    [Fact]
    public async Task ToolExecutor_ExecutesMultipleTools_InCorrectOrder()
    {
        // Arrange
        var executionOrder = new List<string>();
        var mockExecutor = new Mock<IToolExecutor>();

        mockExecutor.Setup(x => x.ExecuteAsync(It.IsAny<ToolRequest>()))
            .Callback<ToolRequest>(r => executionOrder.Add(r.ToolName))
            .ReturnsAsync(new ToolResult { Success = true });

        var orchestrator = new ToolOrchestrator(mockExecutor.Object);

        var plan = new ExecutionPlan
        {
            Steps = new[]
            {
                new PlanStep { Tool = "Tool1", DependsOn = null },
                new PlanStep { Tool = "Tool2", DependsOn = "Tool1" },
                new PlanStep { Tool = "Tool3", DependsOn = "Tool2" }
            }
        };

        // Act
        await orchestrator.ExecutePlanAsync(plan);

        // Assert
        executionOrder.Should().BeEquivalentTo(new[] { "Tool1", "Tool2", "Tool3" },
            options => options.WithStrictOrdering());
    }
}
```

## 4. End-to-End Tests (10% of tests)

### A. Full User Journey Tests

```csharp
public class E2EUserJourneyTests : IClassFixture<MiboTestEnvironment>
{
    private readonly MiboTestEnvironment _environment;

    [Fact]
    public async Task CompleteUserJourney_FromRegistrationToConversation()
    {
        // Step 1: Register new user
        var registrationResponse = await _environment.ApiClient.PostAsJsonAsync("/api/auth/register",
            new { Email = "newuser@test.com", Password = "Test123!", Username = "testuser" });

        registrationResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Step 2: Login
        var loginResponse = await _environment.ApiClient.PostAsJsonAsync("/api/auth/login",
            new { Email = "newuser@test.com", Password = "Test123!" });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult.Token.Should().NotBeEmpty();

        // Step 3: Start conversation
        _environment.ApiClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult.Token);

        var conversationResponse = await _environment.ApiClient.PostAsJsonAsync("/api/conversations/create",
            new { Title = "Test Conversation" });

        var conversation = await conversationResponse.Content.ReadFromJsonAsync<Conversation>();

        // Step 4: Send message
        var messageResponse = await _environment.ApiClient.PostAsJsonAsync(
            $"/api/conversations/{conversation.Id}/messages",
            new { Content = "What's my account balance?" });

        var message = await messageResponse.Content.ReadFromJsonAsync<Message>();

        // Step 5: Verify AI response
        await Task.Delay(2000); // Wait for AI processing

        var messagesResponse = await _environment.ApiClient.GetAsync(
            $"/api/conversations/{conversation.Id}/messages");

        var messages = await messagesResponse.Content.ReadFromJsonAsync<List<Message>>();
        messages.Should().HaveCountGreaterThan(1);
        messages.Last().Role.Should().Be("assistant");
        messages.Last().Content.Should().Contain("balance");
    }
}
```

### B. Performance Tests

```csharp
public class PerformanceTests
{
    [Fact]
    public async Task API_HandlesConcurrentRequests_UnderLoad()
    {
        // Arrange
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var token = await GetAuthTokenAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act - Simulate 100 concurrent users
        var tasks = Enumerable.Range(1, 100).Select(async i =>
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await client.GetAsync($"/api/accounts/{i % 10 + 1}");
            stopwatch.Stop();

            return new
            {
                StatusCode = response.StatusCode,
                ResponseTime = stopwatch.ElapsedMilliseconds
            };
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Count(r => r.StatusCode == HttpStatusCode.OK).Should().BeGreaterThan(95);
        results.Average(r => r.ResponseTime).Should().BeLessThan(200); // avg < 200ms
        results.Max(r => r.ResponseTime).Should().BeLessThan(1000); // max < 1s
    }

    [Fact]
    public async Task ConversationProcessing_MeetsLatencyRequirements()
    {
        // Arrange
        var service = new ConversationService(/* dependencies */);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var result = await service.ProcessMessageAsync("Hello", 1);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // < 500ms
    }
}
```

## 5. Contract Tests

### A. API Contract Tests with Pact

```csharp
public class ApiContractTests
{
    [Fact]
    public void FinanceService_Contract_IsValid()
    {
        // Consumer side
        var pact = new PactBuilder()
            .Consumer("ConversationService")
            .HasPactWith("FinanceDataService");

        pact.MockService(1234)
            .Given("User 1 has accounts")
            .UponReceiving("A request for user accounts")
            .With(new ProviderServiceRequest
            {
                Method = HttpVerb.Get,
                Path = "/api/accounts/user/1",
                Headers = new Dictionary<string, object>
                {
                    { "Accept", "application/json" }
                }
            })
            .WillRespondWith(new ProviderServiceResponse
            {
                Status = 200,
                Headers = new Dictionary<string, object>
                {
                    { "Content-Type", "application/json; charset=utf-8" }
                },
                Body = new
                {
                    accounts = new[]
                    {
                        new
                        {
                            id = 1,
                            userId = 1,
                            accountNumber = Match.Type("123456"),
                            balance = Match.Decimal(1000.00)
                        }
                    },
                    total = 1
                }
            });

        pact.Verify();
    }
}
```

## 6. Security Tests

### A. Authentication & Authorization Tests

```csharp
public class SecurityTests
{
    [Fact]
    public async Task API_PreventsSQLInjection()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var maliciousInput = "1; DROP TABLE Users; --";

        // Act
        var response = await client.GetAsync($"/api/accounts/{maliciousInput}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        // Verify database tables still exist
        var tablesExist = await VerifyDatabaseIntegrityAsync();
        tablesExist.Should().BeTrue();
    }

    [Fact]
    public async Task JWT_ExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        var expiredToken = GenerateExpiredToken();
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await client.GetAsync("/api/accounts/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32\\config\\sam")]
    public async Task API_PreventsPa thTraversal(string maliciousPath)
    {
        // Act
        var response = await _client.GetAsync($"/api/files/{maliciousPath}");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Forbidden
        );
    }
}
```

## 7. Chaos Engineering Tests

### A. Resilience Tests

```csharp
public class ChaosTests
{
    [Fact]
    public async Task System_RecoversfromDatabaseOutage()
    {
        // Arrange
        var chaosMonkey = new ChaosMonkey();

        // Act - Kill database connection
        await chaosMonkey.KillDatabaseConnectionAsync();

        // Try to use the system
        var response = await TryGetAccountWithRetryAsync();

        // Assert - System should recover
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CircuitBreaker_OpensOnRepeatedFailures()
    {
        // Arrange
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.SetupSequence(/*...*/)
            .Returns(HttpStatusCode.InternalServerError)
            .Returns(HttpStatusCode.InternalServerError)
            .Returns(HttpStatusCode.InternalServerError)
            .Returns(HttpStatusCode.OK);

        var client = new HttpClient(mockHttpHandler.Object);
        var service = new ExternalApiService(client);

        // Act
        var results = new List<bool>();
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await service.CallExternalApiAsync();
                results.Add(true);
            }
            catch (CircuitBreakerOpenException)
            {
                results.Add(false);
            }
        }

        // Assert - Circuit should open after 3 failures
        results.Should().BeEquivalentTo(new[] { false, false, false, false, false });
    }
}
```

## Test Infrastructure Setup

### Create Test Solution Structure
```bash
# Create test projects
dotnet new sln -n MIBO.Tests
dotnet new xunit -n MIBO.UnitTests
dotnet new xunit -n MIBO.IntegrationTests
dotnet new xunit -n MIBO.E2ETests
dotnet new xunit -n MIBO.PerformanceTests

# Add to solution
dotnet sln add MIBO.UnitTests/MIBO.UnitTests.csproj
dotnet sln add MIBO.IntegrationTests/MIBO.IntegrationTests.csproj
dotnet sln add MIBO.E2ETests/MIBO.E2ETests.csproj
dotnet sln add MIBO.PerformanceTests/MIBO.PerformanceTests.csproj

# Add common test packages
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package AutoFixture
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers
dotnet add package WireMock.Net
dotnet add package NBomber  # For load testing
```

### GitHub Actions CI Pipeline
```yaml
name: Test Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: postgres
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

      mongodb:
        image: mongo:7
        options: >-
          --health-cmd "mongosh --eval 'db.adminCommand(\"ping\")'"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

      redis:
        image: redis:7
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run Unit Tests
      run: dotnet test MIBO.UnitTests --no-build --verbosity normal --collect:"XPlat Code Coverage"

    - name: Run Integration Tests
      run: dotnet test MIBO.IntegrationTests --no-build --verbosity normal
      env:
        ConnectionStrings__Postgres: "Host=localhost;Database=test;Username=postgres;Password=postgres"
        ConnectionStrings__MongoDB: "mongodb://localhost:27017"
        ConnectionStrings__Redis: "localhost:6379"

    - name: Run E2E Tests
      run: dotnet test MIBO.E2ETests --no-build --verbosity normal

    - name: Generate Coverage Report
      uses: codecov/codecov-action@v3
      with:
        file: ./coverage.cobertura.xml

    - name: Run Performance Tests
      run: dotnet test MIBO.PerformanceTests --no-build --verbosity normal
      continue-on-error: true  # Don't fail build on perf regression
```

## Test Coverage Goals

- **Unit Tests**: 80% code coverage
- **Integration Tests**: All critical paths covered
- **E2E Tests**: Main user journeys
- **Performance Tests**: P95 latency < 200ms
- **Overall Coverage Target**: 70%

## Testing Best Practices for MIBO

1. **Test Naming Convention**: `MethodName_StateUnderTest_ExpectedBehavior`
2. **AAA Pattern**: Arrange, Act, Assert
3. **One Assert Per Test**: Keep tests focused
4. **Use Test Data Builders**: For complex object creation
5. **Mock External Dependencies**: Don't hit real APIs in tests
6. **Test Both Happy and Unhappy Paths**
7. **Use Parameterized Tests**: For multiple scenarios
8. **Keep Tests Fast**: < 100ms for unit tests
9. **Tests Should Be Independent**: No shared state
10. **Use Continuous Testing**: Run on every commit