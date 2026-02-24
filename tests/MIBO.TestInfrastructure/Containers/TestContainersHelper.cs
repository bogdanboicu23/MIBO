using System;
using System.Threading.Tasks;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Testcontainers.Nats;
using DotNet.Testcontainers.Containers;

namespace MIBO.TestInfrastructure.Containers;

public class TestContainersHelper : IAsyncDisposable
{
    private PostgreSqlContainer? _postgresContainer;
    private MongoDbContainer? _mongoContainer;
    private RedisContainer? _redisContainer;
    private NatsContainer? _natsContainer;

    public string PostgresConnectionString => _postgresContainer?.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL container not started");

    public string MongoConnectionString => _mongoContainer?.GetConnectionString()
        ?? throw new InvalidOperationException("MongoDB container not started");

    public string RedisConnectionString => _redisContainer?.GetConnectionString()
        ?? throw new InvalidOperationException("Redis container not started");

    public string NatsConnectionString => _natsContainer?.GetConnectionString()
        ?? throw new InvalidOperationException("NATS container not started");

    public async Task StartPostgreSqlAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("mibo_test")
            .WithUsername("test")
            .WithPassword("test123")
            .Build();

        await _postgresContainer.StartAsync();
    }

    public async Task StartMongoDbAsync()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7")
            .Build();

        await _mongoContainer.StartAsync();
    }

    public async Task StartRedisAsync()
    {
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _redisContainer.StartAsync();
    }

    public async Task StartNatsAsync()
    {
        _natsContainer = new NatsBuilder()
            .WithImage("nats:2.10-alpine")
            .Build();

        await _natsContainer.StartAsync();
    }

    public async Task StartAllAsync()
    {
        var tasks = new[]
        {
            StartPostgreSqlAsync(),
            StartMongoDbAsync(),
            StartRedisAsync(),
            StartNatsAsync()
        };

        await Task.WhenAll(tasks);
    }

    public async ValueTask DisposeAsync()
    {
        var tasks = new[]
        {
            _postgresContainer?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            _mongoContainer?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            _redisContainer?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            _natsContainer?.DisposeAsync().AsTask() ?? Task.CompletedTask
        };

        await Task.WhenAll(tasks);
    }
}