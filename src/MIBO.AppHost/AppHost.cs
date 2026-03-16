using System.Text.Json;
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

var conversationSettingsPath = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "MIBO.ConversationService", "appsettings.json"));
var mongoConnectionString = builder.Configuration["Mongo:ConnectionString"];
var mongoDatabase = builder.Configuration["Mongo:Database"] ?? "mibo";
var mongoConversationsCollection = builder.Configuration["Mongo:ConversationsCollection"] ?? "conversations";
var mongoMessagesCollection = builder.Configuration["Mongo:MessagesCollection"] ?? "messages";
var groqApiKey = ResolveConfigValue(builder.Configuration, "Groq:ApiKey", "GROQ_API_KEY");

if ((string.IsNullOrWhiteSpace(mongoConnectionString)
        || string.IsNullOrWhiteSpace(mongoDatabase)
        || string.IsNullOrWhiteSpace(mongoConversationsCollection)
        || string.IsNullOrWhiteSpace(mongoMessagesCollection))
    && File.Exists(conversationSettingsPath))
{
    using var settingsStream = File.OpenRead(conversationSettingsPath);
    using var settingsDocument = JsonDocument.Parse(settingsStream);
    if (settingsDocument.RootElement.TryGetProperty("Mongo", out var mongoSection))
    {
        if (string.IsNullOrWhiteSpace(mongoConnectionString)
            && mongoSection.TryGetProperty("ConnectionString", out var connectionStringElement))
        {
            mongoConnectionString = connectionStringElement.GetString();
        }

        if (string.IsNullOrWhiteSpace(mongoDatabase)
            && mongoSection.TryGetProperty("Database", out var databaseElement)
            && !string.IsNullOrWhiteSpace(databaseElement.GetString()))
        {
            mongoDatabase = databaseElement.GetString()!;
        }

        if (string.IsNullOrWhiteSpace(mongoConversationsCollection)
            && mongoSection.TryGetProperty("ConversationsCollection", out var collectionElement)
            && !string.IsNullOrWhiteSpace(collectionElement.GetString()))
        {
            mongoConversationsCollection = collectionElement.GetString()!;
        }

        if (string.IsNullOrWhiteSpace(mongoMessagesCollection)
            && mongoSection.TryGetProperty("MessagesCollection", out var messagesCollectionElement)
            && !string.IsNullOrWhiteSpace(messagesCollectionElement.GetString()))
        {
            mongoMessagesCollection = messagesCollectionElement.GetString()!;
        }
    }
}

if (string.IsNullOrWhiteSpace(mongoConnectionString))
{
    throw new InvalidOperationException(
        $"Mongo:ConnectionString is required to start langchain-service. Checked '{conversationSettingsPath}'.");
}

if (string.IsNullOrWhiteSpace(groqApiKey))
{
    throw new InvalidOperationException(
        "Groq:ApiKey or GROQ_API_KEY is required to start langchain-service from AppHost.");
}

// Infrastructure
var redis = builder.AddRedis("redis");
var postgres = builder.AddPostgres("postgres");
var identityDb = postgres.AddDatabase("identitydb", "mibo_identity");

var nats = builder.AddContainer("nats", "nats:2.10-alpine")
    .WithArgs("-js")
    .WithEndpoint(name: "client", port: 4222, targetPort: 4222)
    .WithEndpoint(name: "monitor", port: 8222, targetPort: 8222);

// Backend services
var actionService = builder.AddProject<Projects.MIBO_ActionService>("action-service");

var langchain = builder.AddDockerfile("langchain-service", "../MIBO.LangChainService")
    .WithReference(actionService)
    .WithEnvironment("PORT", "8088")
    .WithEnvironment("GROQ_API_KEY", groqApiKey)
    .WithEnvironment("CONTEXT_CONFIG_DIR", "/app/config")
    .WithEnvironment("ACTION_SERVICE_URL", actionService.GetEndpoint("http"))
    .WithEnvironment("MONGO_URI", mongoConnectionString)
    .WithEnvironment("MONGO_DATABASE", mongoDatabase)
    .WithEnvironment("MONGO_CONVERSATIONS_COLLECTION", mongoConversationsCollection)
    .WithEnvironment("MONGO_MESSAGES_COLLECTION", mongoMessagesCollection)
    .WithEnvironment("Mongo__ConnectionString", mongoConnectionString)
    .WithEnvironment("Mongo__Database", mongoDatabase)
    .WithEnvironment("Mongo__ConversationsCollection", mongoConversationsCollection)
    .WithEnvironment("Mongo__MessagesCollection", mongoMessagesCollection)
    .WithHttpEndpoint(name: "http", port: 8088, targetPort: 8088)
    .WaitFor(actionService)
    .WithExternalHttpEndpoints();

var conversation = builder.AddProject<Projects.MIBO_ConversationService>("conversation");
conversation
    .WithReference(redis)
    .WithEnvironment("ConnectionStrings__Redis", redis)
    .WithEnvironment("AGENT_SERVICE_URL", langchain.GetEndpoint("http"))
    .WithEnvironment("ACTION_SERVICE_URL", actionService.GetEndpoint("http"))
    .WithEnvironment("Planner__BaseUrl", langchain.GetEndpoint("http"))
    .WithEnvironment("NatsJetStream__Url", "nats://localhost:4222")
    .WaitFor(redis)
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

builder.Build().Run();
