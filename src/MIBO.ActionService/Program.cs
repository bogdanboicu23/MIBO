using MIBO.ActionService.Services;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddHttpClient("dummyjson", (serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["DUMMYJSON_BASE_URL"] ?? "https://dummyjson.com";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
});

builder.Services.AddSingleton<IActionRouter, ActionRouter>();

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();