
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
// var redis = builder.AddRedis("redis");
// var postgres = builder.AddPostgres("postgres");

// Backend services
var conversation = builder.AddProject<Projects.MIBO_ConversationService>("conversation");
    // .WithReference(redis);

var identityService = builder.AddProject<Projects.MIBO_IdentityService>("identity-service");
    // .WithReference(postgres);

// API Gateway
    var apiGateway = builder.AddProject<Projects.MIBO_ApiGateway>("api-gateway")
        .WithReference(conversation)
        .WithReference(identityService);

// Frontend client
builder.AddJavaScriptApp("client", "../MIBO.Client/client")
    .WithReference(apiGateway)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();