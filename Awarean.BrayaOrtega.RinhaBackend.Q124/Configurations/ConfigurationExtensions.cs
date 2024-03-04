using System.Threading.Channels;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Services;
using NATS.Client.Core;
using Npgsql;
using StackExchange.Redis;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        services.AddSingleton<NpgsqlDataSource>(x => new NpgsqlDataSourceBuilder(connectionString).Build());

        AddTransactionService(services, configuration);
        services.AddScoped<IRepository, Repository>();

        return services;
    }

    private static void AddTransactionService(IServiceCollection services, IConfiguration configuration)
    {
        var cacheConnectionString = configuration.GetConnectionString("Redis");

        services.AddSingleton(_ => ConnectionMultiplexer.Connect(cacheConnectionString, x =>
        {
            x.ConnectRetry = 10;

            x.AsyncTimeout = 10000;
            x.KeepAlive = 180;
        }));

        services.AddSingleton<Channel<int>>(_ => Channel.CreateUnbounded<int>());
        ConfigureNats(services, configuration);

        services.AddScoped<ICachedRepository, CachedRepository>();

        services.AddScoped<ITransactionService, TransactionService>();
    }

    private static IServiceCollection ConfigureNats(IServiceCollection services, IConfiguration configuration)
    {
        string natsConnectionString = configuration.GetConnectionString("Nats");
        string natsDestinationQueue = configuration["NATS_DESTINATION"];
        string natsOwnQueue = configuration["NATS_OWN"];

        services.AddKeyedSingleton("Nats", natsConnectionString);
        services.AddKeyedSingleton("NatsDestination", natsDestinationQueue);
        services.AddKeyedSingleton("NatsOwnChannel", natsOwnQueue);

        var opts = new NatsOpts() with
        {
            Url = natsConnectionString,
            ObjectPoolSize = 50000,
            SerializerRegistry = new NatsJsonContextSerializerRegistry(AppJsonSerializerContext.Default),
        };

        services.AddSingleton<INatsConnection>(_ => new NatsConnection(opts));

        return services;
    }

    public static IServiceCollection ConfigureBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<SaveInBackgroundHostedService>();
        services.AddHostedService<InitializeRedisBackgroundService>();
        return services;
    }
}
