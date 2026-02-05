using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var redis = builder.AddRedis("redis");
var postgres = builder.AddPostgres("postgres");

// Backend services
var conversation = builder.AddProject<Projects.MIBO_ConversationService>("conversation")
    .WithReference(redis);

var visualization = builder.AddProject<Projects.MIBO_VisualisationService>("visualization");

var calendarAgent = builder.AddProject<Projects.MIBO_CalendarAgent>("calendar-agent")
    .WithReference(postgres);

// API Gateway
builder.AddProject<Projects.MIBO_ApiGateway>("api-gateway")
    .WithReference(conversation)
    .WithReference(visualization)
    .WithReference(calendarAgent);

// Python agents
builder.AddExecutable("finance-agent", "python", "../MIBO.Agents/FinanceAgent", "main.py");

builder.AddExecutable("weather-agent", "python", "../MIBO.Agents/WeatherAgent", "main.py");

builder.Build().Run();