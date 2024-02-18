using System.Diagnostics;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Microsoft.EntityFrameworkCore;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;

public static class ConfigurationExtensions
{
    public static IServiceCollection ConfigureInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextPool<RinhaBackendDbContext>(x =>
        {
            x.UseNpgsql(connectionString);
            x.EnableServiceProviderCaching();
            x.EnableThreadSafetyChecks();
            x.LogTo(Console.WriteLine);
            x.LogTo(x => Debug.WriteLine(x));
            x.LogTo(x => Trace.WriteLine(x));
            x.EnableSensitiveDataLogging();
        });

        services.AddScoped<Repository>();

        return services;
    }
}
