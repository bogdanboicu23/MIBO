
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var redis = builder.AddRedis("redis");
var postgres = builder.AddPostgres("postgres");
var identityDb = postgres.AddDatabase("identitydb", "mibo_identity");

var nats = builder.AddContainer("nats", "nats:2.10-alpine")
    .WithArgs("-js")
    .WithEndpoint(name: "client", port: 4222, targetPort: 4222)
    .WithEndpoint(name: "monitor", port: 8222, targetPort: 8222);

var langchain = builder.AddDockerfile("langchain-service", "../MIBO.LangChainService")
    .WithEnvironment("PORT", "8088")
    .WithEnvironment("CONTEXT_CONFIG_DIR", "/app/config")
    .WithHttpEndpoint(name: "http", port: 8088, targetPort: 8088)
    .WithExternalHttpEndpoints();

// Backend services
var conversation = builder.AddProject<Projects.MIBO_ConversationService>("conversation");
conversation
    .WithReference(redis)
    .WithEnvironment("ConnectionStrings__Redis", redis)
    .WithEnvironment("Planner__BaseUrl", "http://localhost:8088")
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
    .WithReference(conversation)
    .WithReference(identityService)
    .WaitFor(conversation)
    .WaitFor(identityService);

// Frontend client
builder.AddJavaScriptApp("client", "../MIBO.Client/client")
    .WithReference(apiGateway)
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
