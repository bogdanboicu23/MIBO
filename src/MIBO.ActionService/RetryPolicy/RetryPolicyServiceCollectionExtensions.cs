using MIBO.Storage.Mongo;
using MIBO.Storage.Mongo.Integrations;

namespace MIBO.ActionService.RetryPolicy;

public static class RetryPolicyServiceCollectionExtensions
{
    public static IServiceCollection AddExternalServiceMonitoring(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RetryPolicyOptions>()
            .Bind(configuration.GetSection(RetryPolicyOptions.SectionName));

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName));

        services.PostConfigure<RabbitMqOptions>(options =>
        {
            var connectionString = configuration.GetConnectionString("rabbitmq");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.ConnectionString = connectionString;
            }
        });

        services.AddSingleton<IExternalServiceRegistry, ExternalServiceRegistry>();
        services.AddSingleton<IExternalServiceStatusQuery, ExternalServiceStatusQuery>();

        var retryOptions = configuration
            .GetSection(RetryPolicyOptions.SectionName)
            .Get<RetryPolicyOptions>() ?? new RetryPolicyOptions();
        var mongoConnectionString = configuration["Mongo:ConnectionString"];
        var shouldUseMongo = retryOptions.AuditEnabled
            || retryOptions.StatusPageEnabled
            || (retryOptions.Enabled && retryOptions.UseRabbit);

        if (shouldUseMongo && !string.IsNullOrWhiteSpace(mongoConnectionString))
        {
            services.AddMongo(configuration);
        }
        else
        {
            services.AddSingleton<IExternalServiceMonitorStore, NoOpExternalServiceMonitorStore>();
        }

        var shouldUseExecutor = retryOptions.Enabled
            || retryOptions.AuditEnabled
            || retryOptions.StatusPageEnabled;
        var shouldUseRabbitConsumer = retryOptions.Enabled && retryOptions.UseRabbit;

        if (retryOptions.UseRabbit)
        {
            services.AddSingleton<IRabbitMqRetryPublisher, RabbitMqRetryPublisher>();
        }
        else
        {
            services.AddSingleton<IRabbitMqRetryPublisher, NoOpRabbitMqRetryPublisher>();
        }

        if (shouldUseExecutor)
        {
            services.AddSingleton<IExternalServiceExecutor, ExternalServiceExecutor>();
        }
        else
        {
            services.AddSingleton<IExternalServiceExecutor, NoOpExternalServiceExecutor>();
        }

        if (shouldUseRabbitConsumer)
        {
            services.AddHostedService<RabbitMqRetryConsumerHostedService>();
        }

        return services;
    }
}
