using FluentAssertions;
using MIBO.Storage.Mongo.Conversations;
using MIBO.Storage.Mongo.Store.Conversation;
using MIBO.Storage.Mongo.Store.Ui;
using MIBO.Storage.Mongo.Store.UiSubscription;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace MIBO.Storage.Mongo.Tests.Unit;

public class MongoBootstrapTests
{
    private static (IServiceCollection Services, IConfiguration Config) CreateServicesWithConfig(
        string connectionString = "mongodb://localhost:27017",
        string database = "test_db")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = connectionString,
                ["Mongo:Database"] = database
            })
            .Build();

        var services = new ServiceCollection();
        return (services, config);
    }

    // ════════════════════════════════════════════
    //  Service registrations
    // ════════════════════════════════════════════

    [Fact]
    public void AddMongo_RegistersMongoClientAsSingleton()
    {
        var (services, config) = CreateServicesWithConfig();

        services.AddMongo(config);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IMongoClient) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMongo_RegistersMongoDatabaseAsSingleton()
    {
        var (services, config) = CreateServicesWithConfig();

        services.AddMongo(config);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IMongoDatabase) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMongo_RegistersConversationRepositoryAsSingleton()
    {
        var (services, config) = CreateServicesWithConfig();

        services.AddMongo(config);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IConversationRepository) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMongo_RegistersConversationStoreAsSingleton()
    {
        var (services, config) = CreateServicesWithConfig();

        services.AddMongo(config);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IConversationStore) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMongo_RegistersUiInstanceStoreAsSingleton()
    {
        var (services, config) = CreateServicesWithConfig();

        services.AddMongo(config);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IUiInstanceStore) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMongo_RegistersUiSubscriptionStoreAsSingleton()
    {
        var (services, config) = CreateServicesWithConfig();

        services.AddMongo(config);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IUiSubscriptionStore) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddMongo_RegistersMongoIndexHostedService()
    {
        var (services, config) = CreateServicesWithConfig();

        services.AddMongo(config);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IHostedService));
    }

    // ════════════════════════════════════════════
    //  Fluent return
    // ════════════════════════════════════════════

    [Fact]
    public void AddMongo_ReturnsSameServiceCollection()
    {
        var (services, config) = CreateServicesWithConfig();

        var result = services.AddMongo(config);

        result.Should().BeSameAs(services);
    }

    // ════════════════════════════════════════════
    //  Factory validation
    // ════════════════════════════════════════════

    [Fact]
    public void AddMongo_EmptyConnectionString_MongoClientFactoryRegistered()
    {
        var (services, config) = CreateServicesWithConfig(connectionString: "");

        services.AddMongo(config);

        // The factory is registered — it will throw at resolution time when ConnectionString is empty.
        // We verify the descriptor exists with the correct lifetime.
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IMongoClient) &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }
}
