using System.Threading.Channels;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using Npgsql;
using StackExchange.Redis;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        var cacheConnectionString = configuration.GetConnectionString("Redis");
        services.AddSingleton<NpgsqlDataSource>(x => new NpgsqlDataSourceBuilder(connectionString).Build());

        if (configuration.GetValue<bool>("BYPASS_CACHE"))
        {
            services.AddScoped<IDecoratedRepository, Repository>();
        }
        else
        {
            AddRedisCacheRepository(services, cacheConnectionString);
        }

        services.AddScoped<IRepository, Repository>();

        return services;
    }

    private static void AddRedisCacheRepository(IServiceCollection services, string cacheConnectionString)
    {
        services.AddSingleton(_ => ConnectionMultiplexer.Connect(cacheConnectionString, x =>
        {
            x.ConnectRetry = 10;

            x.AsyncTimeout = 10000;
            x.KeepAlive = 180;
        }));

        services.AddScoped<IDecoratedRepository, CacheRepository>();
    }

    public static IServiceCollection ConfigureMessaging(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<Channel<int>>(_ => Channel.CreateUnbounded<int>());
        return ConfigureNats(services, config);
    }

    private static IServiceCollection ConfigureNats(IServiceCollection services, IConfiguration config)
    {
        string natsConnectionString = config.GetConnectionString("Nats");
        string natsDestinationQueue = config["NATS_DESTINATION"];
        string natsOwnQueue = config["NATS_OWN"];
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
        services.AddSingleton<INatsKVStore>(provider =>
        {
            var conn = provider.GetRequiredService<INatsConnection>();
            var js = new NatsJSContext((NatsConnection)conn);
            var ctx = new NatsKVContext(js);

            var store = ctx.CreateStoreAsync(nameof(Account));
            while (store.IsCompleted is false) { }

            return store.Result;
        });

        return services;
    }

    public static IServiceCollection ConfigureBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<SaveInBackgroundHostedService>();

        return services;
    }
}
