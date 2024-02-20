using System.Diagnostics;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
//using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra.CompiledModels;
//using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureInfrastructure(this IServiceCollection services, string connectionString)
    {
        // services.AddDbContextPool<RinhaBackendDbContext>(x =>
        // {
        //     x.UseModel(RinhaBackendDbContextModel.Instance);
        //     x.UseNpgsql(connectionString, x=> x.EnableRetryOnFailure(50, TimeSpan.FromMilliseconds(50), []));
        //     x.EnableServiceProviderCaching();
        //     x.EnableThreadSafetyChecks();
        //     x.LogTo(Console.WriteLine);
        //     x.LogTo(x => Debug.WriteLine(x));
        //     x.LogTo(x => Trace.WriteLine(x));
        //     x.EnableSensitiveDataLogging();
        // });

        services.AddScoped<NpgsqlDataSource>(x => new NpgsqlDataSourceBuilder(connectionString).Build());
        services.AddScoped<Repository>();

        return services;
    }
}
