using System.Diagnostics;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
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

        services.AddScoped<IRepository, Repository>();

        services.AddSingleton(_ => ConnectionMultiplexer.Connect(cacheConnectionString, x =>
        {
            x.ConnectRetry = 5;
            x.AsyncTimeout = 10000;
            x.KeepAlive = 180;
        }));

        services.AddScoped<IDecoratedRepository, CacheRepository>();

        return services;
    }
}
