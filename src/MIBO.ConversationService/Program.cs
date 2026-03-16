using MIBO.Storage.Mongo;

var builder = WebApplication.CreateBuilder(args);

static string ResolveBaseUrl(IConfiguration configuration, string defaultUrl, params string[] keys)
{
    foreach (var key in keys)
    {
        var value = configuration[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    return defaultUrl;
}

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddMongo(builder.Configuration);

builder.Services.AddHttpClient("agent", (serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = ResolveBaseUrl(
        configuration,
        "http://localhost:8088",
        "AGENT_SERVICE_URL",
        "Planner:BaseUrl",
        "services:langchain-service:http:0");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = Timeout.InfiniteTimeSpan;
});

builder.Services.AddHttpClient("actions", (serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = ResolveBaseUrl(
        configuration,
        "http://localhost:5288",
        "ACTION_SERVICE_URL",
        "services:action-service:http:0");
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();
