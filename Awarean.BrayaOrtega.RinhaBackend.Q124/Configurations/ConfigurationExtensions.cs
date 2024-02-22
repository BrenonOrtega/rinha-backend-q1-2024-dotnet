using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

//using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra.CompiledModels;
//using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackExchange.Redis;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureInfrastructure(this IServiceCollection services,
     string connectionString,
     string cacheConnectionString)
    {
        services.AddSingleton<NpgsqlDataSource>(x => new NpgsqlDataSourceBuilder(connectionString).Build());

        services.AddScoped<IDecoratedRepository, CacheRepository>();
        services.AddStackExchangeRedisCache(x =>
        {
            x.Configuration = cacheConnectionString;
        });

        services.AddScoped<IRepository, Repository>();

        // services.AddSingleton(_ => ConnectionMultiplexer.ConnectAsync(cacheConnectionString, x =>
        // {
        //     x.ConnectRetry = 3;
        //     x.AsyncTimeout = 30000;
        //     x.SyncTimeout = 30000;
        //     x.KeepAlive = 180;
        //     x.IncludeDetailInExceptions = true;
        //     x.AbortOnConnectFail = true;
        // }).GetAwaiter().GetResult());


        services.AddSingleton(_ => new ConcurrentQueue<Transaction>());

        return services;
    }
}
