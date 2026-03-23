using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MIBO.ApiGateway.Handlers;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;

var builder = WebApplication.CreateBuilder(args);

// Add Ocelot configuration
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("ocelot.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Accept both the legacy Jwt:* layout and the newer JwtSettings:* layout
// so local/dev gateway validation stays aligned with IdentityService.
var jwtKey = ResolveJwtSetting(
    builder.Configuration,
    "Jwt:Key",
    "JwtSettings:AccessTokenSecret");
var jwtIssuer = ResolveJwtSetting(
    builder.Configuration,
    "Jwt:Issuer",
    "JwtSettings:Issuer");
var jwtAudience = ResolveJwtSetting(
    builder.Configuration,
    "Jwt:Audience",
    "JwtSettings:Audience");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return false;
                }

                return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("mibo.monster", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("www.mibo.monster", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("api.mibo.monster", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("status.mibo.monster", StringComparison.OrdinalIgnoreCase);
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add Ocelot with Kubernetes provider
builder.Services.AddTransient<ClaimsToHeadersHandler>();
builder.Services.AddOcelot()
    .AddDelegatingHandler<ClaimsToHeadersHandler>(global: true)
    .AddKubernetes();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Add Ocelot middleware
app.UseWebSockets();
await app.UseOcelot();

app.Run();

static string ResolveJwtSetting(IConfiguration configuration, params string[] keys)
{
    foreach (var key in keys)
    {
        var value = configuration[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    throw new InvalidOperationException(
        $"Missing JWT configuration. Expected one of: {string.Join(", ", keys)}");
}
