using System.Net.Http.Headers;
using MIBO.Cache.Redis.Tools;
using MIBO.ConversationService.DTOs.Eventing;
using MIBO.ConversationService.DTOs.Options;
using MIBO.ConversationService.Helper;
using MIBO.ConversationService.Hubs;
using MIBO.ConversationService.Middleware.Http;
using MIBO.ConversationService.Services.Actions.Handler;
using MIBO.ConversationService.Services.Actions.Router;
using MIBO.ConversationService.Services.Actions.RoutingSpecProvider;
using MIBO.ConversationService.Services.Answer;
using MIBO.ConversationService.Services.Background;
using MIBO.ConversationService.Services.Chat;
using MIBO.ConversationService.Services.Composer.Text;
using MIBO.ConversationService.Services.Composer.TextSpecProvider;
using MIBO.ConversationService.Services.Composer.Ui;
using MIBO.ConversationService.Services.Composer.UiSpecProvider;
using MIBO.ConversationService.Services.Eventing.Builder;
using MIBO.ConversationService.Services.Eventing.Factory;
using MIBO.ConversationService.Services.Eventing.Handler;
using MIBO.ConversationService.Services.Eventing.JetStream;
using MIBO.ConversationService.Services.GroqChat;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using NATS.Client;

using MIBO.ConversationService.Services.Planner.Client;
using MIBO.ConversationService.Services.Planner.Factory;
using MIBO.ConversationService.Services.Planner.Fallback;
using MIBO.ConversationService.Services.Planner.Validator;
using MIBO.ConversationService.Services.Redis;
using MIBO.ConversationService.Services.Tools;
using MIBO.ConversationService.Services.Tools.BindingResolver;
using MIBO.ConversationService.Services.Tools.PlanExecutor;
using MIBO.ConversationService.Services.UI;
using MIBO.Storage.Mongo;
using MIBO.Storage.Mongo.Store.Conversation;
using MIBO.Storage.Mongo.Store.Ui;
using MIBO.Storage.Mongo.Store.UiSubscription; // LangChainPlannerClient

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "MIBO ConversationService", Version = "v1" });
});

builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 50_000_000);

// ---------- OPTIONS ----------
builder.Services.Configure<ToolCatalogOptions>(builder.Configuration.GetSection("ToolCatalog"));
builder.Services.Configure<ToolExecutorOptions>(builder.Configuration.GetSection("ToolExecutor"));
builder.Services.Configure<PlannerOptions>(builder.Configuration.GetSection("Planner"));
builder.Services.Configure<ChatOrchestratorOptions>(builder.Configuration.GetSection("ChatOrchestrator"));

builder.Services.Configure<UiCatalogOptions>(builder.Configuration.GetSection("UiCatalog"));
builder.Services.Configure<TextComposeSpecOptions>(builder.Configuration.GetSection("TextComposeSpec"));
builder.Services.Configure<UiComposeSpecOptions>(builder.Configuration.GetSection("UiComposeSpec"));

builder.Services.Configure<ActionRoutingOptions>(builder.Configuration.GetSection("ActionRouting"));
builder.Services.Configure<NatsJetStreamOptions>(builder.Configuration.GetSection("NatsJetStream"));

// ---------- HEADER CONTEXT ----------
builder.Services.AddSingleton<HeaderContextMiddleware>();
builder.Services.AddSingleton<IHeaderContextAccessor>(sp => sp.GetRequiredService<HeaderContextMiddleware>());
builder.Services.AddTransient<HeaderContextMiddleware>();

// ---------- REDIS ----------
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var cs = builder.Configuration.GetConnectionString("Redis")
             ?? throw new InvalidOperationException("Missing ConnectionStrings:Redis");
    return ConnectionMultiplexer.Connect(cs);
});
builder.Services.AddSingleton<IToolCache, RedisToolCache>();
builder.Services.AddSingleton<SingleFlight>();

// ---------- HTTP ----------
builder.Services.AddHttpClient("tools", c =>
{
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient<IPlannerClient, LangChainPlannerClient>(c =>
{
    var baseUrl = builder.Configuration["Planner:BaseUrl"]
        ?? throw new InvalidOperationException("Missing Planner:BaseUrl");
    c.BaseAddress = new Uri(baseUrl);
    c.Timeout = TimeSpan.FromSeconds(int.TryParse(builder.Configuration["Planner:TimeoutSeconds"], out var t) ? t : 10);
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// ---------- TOOLS ----------
builder.Services.AddSingleton<IToolCatalogProvider, JsonFileToolCatalogProvider>();
builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();
builder.Services.AddHostedService<ToolRegistryRefresher>();

builder.Services.AddSingleton<IToolPolicyProvider, DefaultToolPolicyProvider>();
builder.Services.AddSingleton<IToolCacheKeyStrategy, DefaultToolCacheKeyStrategy>();
builder.Services.AddSingleton<IArgBindingResolver, NoOpArgBindingResolver>();

builder.Services.AddSingleton<IToolExecutor, ToolExecutor>();

// ---------- UI / COMPOSE ----------
builder.Services.AddSingleton<IUiCatalogProvider, JsonFileUiCatalogProvider>();
builder.Services.AddSingleton<ITextComposeSpecProvider, JsonFileTextComposeSpecProvider>();
builder.Services.AddSingleton<IUiComposeSpecProvider, JsonFileUiComposeSpecProvider>();
builder.Services.AddScoped<ITextComposer, TextComposer>();
builder.Services.AddScoped<IUiComposer, UiComposer>();

// ---------- PLANNER INPUT ----------
builder.Services.AddScoped<IPlannerInputFactory, PlannerInputFactory>();

// ---------- MONGO ----------
builder.Services.AddMongo(builder.Configuration);
builder.Services.AddScoped<IConversationStore, MongoConversationStore>();
builder.Services.AddScoped<IUiInstanceStore, MongoUiInstanceStore>();
builder.Services.AddSingleton<IUiSubscriptionStore, MongoUiSubscriptionStore>();

// ---------- CHAT PIPELINE ----------
builder.Services.AddScoped<IPlanValidator, PlanValidator>();
builder.Services.AddScoped<IToolPlanExecutor, ToolPlanExecutor>();
builder.Services.AddScoped<IFallbackPlanner, RuleBasedFallbackPlanner>();
builder.Services.AddScoped<IChatOrchestrator, ChatOrchestrator>();

// ---------- ACTIONS ----------
builder.Services.AddSingleton<IActionRoutingSpecProvider, JsonFileActionRoutingSpecProvider>();
builder.Services.AddSingleton<IActionRouter, ActionRouter>();
builder.Services.AddScoped<IActionHandler, ActionHandler>();


// ---------- NATS JetStream ----------
builder.Services.AddSingleton<IConnection>(sp =>
{
    var opt = sp.GetRequiredService<IOptions<NatsJetStreamOptions>>().Value;
    var cf = new ConnectionFactory();
    return cf.CreateConnection(opt.Url);
});

builder.Services.AddHostedService<JetStreamBootstrapHostedService>();
builder.Services.AddSingleton<IEventPublisher, JetStreamEventPublisher>();

builder.Services.AddSingleton<IEventHandler, EventRefreshHandler>();
builder.Services.AddHostedService<JetStreamGlobalSubscriberHostedService>();

builder.Services.AddScoped<IEventEnvelopeFactory, DefaultEventEnvelopeFactory>();
builder.Services.AddScoped<IEventPayloadBuilder, DefaultEventPayloadBuilder>();


builder.Services.AddScoped<IAnswerService, GroqAnswerService>();
builder.Services.AddScoped<IGroqChatService, GroqChatService>();
builder.Services.AddHttpClient<IGroqChatService, GroqChatService>(client =>
{
    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
});
builder.Services.AddCors(o =>
{
    o.AddPolicy("cors", p => p
        .WithOrigins("http://localhost:8080")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<HeaderContextMiddleware>();

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("cors");

app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();

app.MapControllers();
app.MapHub<UiHub>("/hubs/ui");

app.Run();
