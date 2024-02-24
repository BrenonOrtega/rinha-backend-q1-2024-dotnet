using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using NATS.Client.Core;



//using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra.CompiledModels;
//using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackExchange.Redis;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureInfrastructure(this IServiceCollection services,
     string connectionString)
    {
        services.AddSingleton<NpgsqlDataSource>(x => new NpgsqlDataSourceBuilder(connectionString).Build());

        services.AddScoped<IDecoratedRepository, CacheRepository>();

        services.AddScoped<IRepository, Repository>();

        services.AddSingleton(_ => new ConcurrentQueue<Transaction>());

        return services;
    }

    public static IServiceCollection ConfigureMessaging(this IServiceCollection services, IConfiguration config)
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

        return services;
    }

    public static IServiceCollection ConfigureBackgroundServices(this IServiceCollection services)
    {
        services.AddSingleton<Repository>((p) => new Repository(p.GetRequiredService<NpgsqlDataSource>()));

        services.AddSingleton<ConcurrentDictionary<int, Account>>(_ => new());
        services.AddHostedService<SaveInBackgroundHostedService>();
        services.AddHostedService<SynchronizeAccountsHostedService>();

        return services;
    }
}
