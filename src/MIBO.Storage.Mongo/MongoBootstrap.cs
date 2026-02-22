using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MIBO.Storage.Mongo;

public static class MongoBootstrap
{
    public static IServiceCollection AddMongo(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<MongoOptions>(cfg.GetSection("Mongo"));

        services.AddSingleton<IMongoClient>(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            if (string.IsNullOrWhiteSpace(opt.ConnectionString))
                throw new InvalidOperationException("Mongo:ConnectionString missing");

            return new MongoClient(opt.ConnectionString);
        });

        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(opt.Database);
        });

        services.AddHostedService<MongoIndexHostedService>();

        return services;
    }
}