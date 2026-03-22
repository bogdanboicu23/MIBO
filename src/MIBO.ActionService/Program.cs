using MIBO.ActionService.ExternalServices;
using MIBO.ActionService.RetryPolicy;
using MIBO.ActionService.Services;
using MIBO.Cache.Redis.Spotify;
using StackExchange.Redis;

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

// Redis + Spotify token infrastructure
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false";
if (!redisConnectionString.Contains("abortConnect", StringComparison.OrdinalIgnoreCase))
    redisConnectionString += ",abortConnect=false";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddOptions<SpotifyClientOptions>()
    .Bind(builder.Configuration.GetSection(SpotifyClientOptions.SectionName));
builder.Services.AddSingleton<ISpotifyTokenStore, RedisSpotifyTokenStore>();
builder.Services.AddSingleton<ISpotifyTokenRefresher, SpotifyTokenRefresher>();
builder.Services.AddHttpClient("spotify");

builder.Services.AddExternalServices(builder.Configuration);
builder.Services.AddExternalServiceMonitoring(builder.Configuration);
builder.Services.AddSingleton<IActionRouter, ActionRouter>();

var app = builder.Build();

app.UseCors();
app.MapControllers();

app.Run();

public partial class Program { }
