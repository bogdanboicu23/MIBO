#!/bin/bash

echo "🚀 Setting up MIBO Test Projects..."

cd /Users/bogdanboicu/Desktop/Universitate/MASTER/MIBO/tests

# Create ConversationService test project
echo "📦 Creating ConversationService test project..."
dotnet new xunit -n MIBO.ConversationService.Tests
cd MIBO.ConversationService.Tests
dotnet add reference ../../src/MIBO.ConversationService/MIBO.ConversationService.csproj
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package AutoFixture
dotnet add package Testcontainers.MongoDb
cd ..

# Create FinanceDataService test project
echo "📦 Creating FinanceDataService test project..."
dotnet new xunit -n MIBO.FinanceDataService.Tests
cd MIBO.FinanceDataService.Tests
dotnet add reference ../../src/MIBO.FinanceDataService/MIBO.FinanceDataService.csproj
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package AutoFixture
cd ..

# Create LangChainService test project
echo "📦 Creating LangChainService test project..."
dotnet new xunit -n MIBO.LangChainService.Tests
cd MIBO.LangChainService.Tests
dotnet add reference ../../src/MIBO.LangChainService/MIBO.LangChainService.csproj
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package WireMock.Net
cd ..

# Create ApiGateway test project
echo "📦 Creating ApiGateway test project..."
dotnet new xunit -n MIBO.ApiGateway.Tests
cd MIBO.ApiGateway.Tests
dotnet add reference ../../src/MIBO.ApiGateway/MIBO.ApiGateway.csproj
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Moq
dotnet add package FluentAssertions
cd ..

# Create shared test infrastructure project
echo "📦 Creating shared test infrastructure..."
dotnet new classlib -n MIBO.TestInfrastructure
cd MIBO.TestInfrastructure
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers
dotnet add package Testcontainers.PostgreSql
dotnet add package Testcontainers.MongoDb
dotnet add package Testcontainers.Redis
dotnet add package Testcontainers.Nats
dotnet add package AutoFixture
dotnet add package FluentAssertions
cd ..

# Create integration tests project
echo "📦 Creating integration tests project..."
dotnet new xunit -n MIBO.IntegrationTests
cd MIBO.IntegrationTests
dotnet add reference ../MIBO.TestInfrastructure/MIBO.TestInfrastructure.csproj
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers
dotnet add package WireMock.Net
cd ..

# Create E2E tests project
echo "📦 Creating E2E tests project..."
dotnet new xunit -n MIBO.E2ETests
cd MIBO.E2ETests
dotnet add reference ../MIBO.TestInfrastructure/MIBO.TestInfrastructure.csproj
dotnet add package Microsoft.Playwright
dotnet add package Testcontainers
cd ..

# Create performance tests project
echo "📦 Creating performance tests project..."
dotnet new xunit -n MIBO.PerformanceTests
cd MIBO.PerformanceTests
dotnet add package NBomber
dotnet add package NBomber.Http
cd ..

# Create test solution
echo "📦 Creating test solution..."
cd /Users/bogdanboicu/Desktop/Universitate/MASTER/MIBO
dotnet new sln -n MIBO.Tests --output tests

# Add all test projects to solution
cd tests
dotnet sln add MIBO.IdentityService.Tests/MIBO.IdentityService.Tests.csproj
dotnet sln add MIBO.ConversationService.Tests/MIBO.ConversationService.Tests.csproj
dotnet sln add MIBO.FinanceDataService.Tests/MIBO.FinanceDataService.Tests.csproj
dotnet sln add MIBO.LangChainService.Tests/MIBO.LangChainService.Tests.csproj
dotnet sln add MIBO.ApiGateway.Tests/MIBO.ApiGateway.Tests.csproj
dotnet sln add MIBO.TestInfrastructure/MIBO.TestInfrastructure.csproj
dotnet sln add MIBO.IntegrationTests/MIBO.IntegrationTests.csproj
dotnet sln add MIBO.E2ETests/MIBO.E2ETests.csproj
dotnet sln add MIBO.PerformanceTests/MIBO.PerformanceTests.csproj

echo "✅ Test projects setup complete!"
echo ""
echo "📁 Test projects created in: /Users/bogdanboicu/Desktop/Universitate/MASTER/MIBO/tests"
echo ""
echo "🧪 To run tests:"
echo "  - All tests: dotnet test tests/MIBO.Tests.sln"
echo "  - Specific service: dotnet test tests/MIBO.IdentityService.Tests"
echo "  - With coverage: dotnet test --collect:'XPlat Code Coverage'"