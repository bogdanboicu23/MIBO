using Microsoft.Extensions.Configuration;


var builder = DistributedApplication.CreateBuilder(args);

static string? ResolveConfigValue(IConfiguration configuration, params string[] keys)
{
    foreach (var key in keys)
    {
        var value = configuration[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    return null;
}

var groqApiKey = ResolveConfigValue(builder.Configuration, "Groq:ApiKey", "GROQ_API_KEY");
var newsApiKey = ResolveConfigValue(builder.Configuration, "ExternalServices:NewsApi:ApiKey", "NEWSAPI_API_KEY");

if (string.IsNullOrWhiteSpace(groqApiKey))
{
    throw new InvalidOperationException(
        "Groq:ApiKey or GROQ_API_KEY is required to start langchain-service from AppHost.");
}

// Infrastructure
var redis = builder.AddRedis("redis").WithoutHttpsCertificate();
var mongo = builder.AddMongoDB("mongodb", port: 27017);
var mongoDatabase = mongo.AddDatabase("miboMongo", "mibo");
var postgres = builder.AddPostgres("postgres");
var identityDb = postgres.AddDatabase("identitydb", "mibo_identity");
var rabbitMq = builder.AddRabbitMQ("rabbitmq", port: 5672)
    .WithManagementPlugin(port: 15672);
var nats = builder.AddContainer("nats", "nats:2.10-alpine")
    .WithArgs("-js")
    .WithEndpoint(name: "client", port: 4222, targetPort: 4222)
    .WithEndpoint(name: "monitor", port: 8222, targetPort: 8222);

// Backend services
var actionService = builder.AddProject<Projects.MIBO_ActionService>("action-service");
actionService
    .WithReference(redis)
    .WithReference(mongoDatabase)
    .WithReference(rabbitMq)
    .WithEnvironment("ConnectionStrings__Redis", redis)
    .WithEnvironment("Mongo__ConnectionString", mongoDatabase)
    .WithEnvironment("Mongo__Database", "mibo")
    .WithEnvironment("RetryPolicy__Enabled", "true")
    .WithEnvironment("RetryPolicy__UseRabbit", "true")
    .WithEnvironment("RetryPolicy__AuditEnabled", "true")
    .WithEnvironment("RetryPolicy__StatusPageEnabled", "true")
    .WithEnvironment("RabbitMq__ConnectionString", rabbitMq)
    .WithEnvironment("BANKSERVICE_BASE_URL", "http://localhost:5286")
    .WithEnvironment("RetryPolicy__MigratedServices__0", "OpenWeatherMap")
    .WithEnvironment("RetryPolicy__MigratedServices__1", "DummyJson")
    .WithEnvironment("RetryPolicy__MigratedServices__2", "Spotify")
    .WithEnvironment("RetryPolicy__MigratedServices__3", "BankService")
    .WithEnvironment("RetryPolicy__MigratedServices__4", "NewsApi")
    .WithEnvironment("NEWSAPI_API_KEY", newsApiKey ?? "")
    .WaitFor(redis)
    .WaitFor(mongo)
    .WaitFor(rabbitMq);

var langchain = builder.AddDockerfile("langchain-service", "../MIBO.LangChainService")
    .WithReference(actionService)
    .WithEnvironment("PORT", "8088")
    .WithEnvironment("GROQ_API_KEY", groqApiKey)
    .WithEnvironment("CONTEXT_CONFIG_DIR", "/app/config")
    .WithEnvironment("ACTION_SERVICE_URL", actionService.GetEndpoint("http"))
    .WithEnvironment("MONGO_URI", mongoDatabase)
    .WithEnvironment("MONGO_DATABASE", "mibo")
    .WithEnvironment("MONGO_CONVERSATIONS_COLLECTION", "conversations")
    .WithEnvironment("MONGO_MESSAGES_COLLECTION", "messages")
    .WithEnvironment("Mongo__ConnectionString", mongoDatabase)
    .WithEnvironment("Mongo__Database", "mibo")
    .WithEnvironment("Mongo__ConversationsCollection", "conversations")
    .WithEnvironment("Mongo__MessagesCollection", "messages")
    .WithHttpEndpoint(name: "http", port: 8088, targetPort: 8088)
    .WaitFor(actionService)
    .WaitFor(mongo)
    .WithExternalHttpEndpoints();

var conversation = builder.AddProject<Projects.MIBO_ConversationService>("conversation");
conversation
    .WithReference(redis)
    .WithReference(mongoDatabase)
    .WithEnvironment("ConnectionStrings__Redis", redis)
    .WithEnvironment("Mongo__ConnectionString", mongoDatabase)
    .WithEnvironment("Mongo__Database", "mibo")
    .WithEnvironment("AGENT_SERVICE_URL", langchain.GetEndpoint("http"))
    .WithEnvironment("ACTION_SERVICE_URL", actionService.GetEndpoint("http"))
    .WithEnvironment("Planner__BaseUrl", langchain.GetEndpoint("http"))
    .WithEnvironment("NatsJetStream__Url", "nats://localhost:4222")
    .WaitFor(redis)
    .WaitFor(mongo)
    .WaitFor(nats)
    .WaitFor(langchain);

var identityService = builder.AddProject<Projects.MIBO_IdentityService>("identity-service");
identityService
    .WithReference(identityDb)
    .WithEnvironment("ConnectionStrings__DefaultConnection", identityDb)
    .WaitFor(postgres);

// API Gateway
var apiGateway = builder.AddProject<Projects.MIBO_ApiGateway>("api-gateway")
    .WithReference(actionService)
    .WithReference(conversation)
    .WithReference(identityService)
    .WaitFor(actionService)
    .WaitFor(conversation)
    .WaitFor(identityService);

// Frontend client
builder.AddJavaScriptApp("client", "../MIBO.Client/client")
    .WithReference(apiGateway)
    .WithEnvironment("VITE_API_SERVER_URL", apiGateway.GetEndpoint("https"))
    .WithEnvironment("VITE_DATA_SERVICE_URL", apiGateway.GetEndpoint("https"))
    .WaitFor(apiGateway)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

// Docs frontend
builder.AddJavaScriptApp("docs", "../MIBO.Client/docs")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.AddJavaScriptApp("status", "../MIBO.Client/status")
    .WithReference(apiGateway)
    .WithEnvironment("VITE_API_SERVER_URL", apiGateway.GetEndpoint("https"))
    .WaitFor(apiGateway)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
