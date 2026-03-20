using MIBO.ActionService.ExternalServices;
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

builder.Services.AddExternalServices(builder.Configuration);
builder.Services.AddSingleton<IActionRouter, ActionRouter>();

var app = builder.Build();

app.UseCors();
app.MapControllers();

app.Run();
