using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Microsoft.AspNetCore.Mvc;
using NATS.Client.Core;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = ConfigureServices(args);

        var app = builder.Build();

        MapEndpoints(app);

        app.Run();
    }

    private static WebApplicationBuilder ConfigureServices(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        builder.Services.AddLogging(x => x.AddConsole());

        builder.Services.ConfigureInfrastructure(builder.Configuration);
        builder.Services.ConfigureMessaging(builder.Configuration);
        builder.Services.ConfigureBackgroundServices();

        return builder;
    }

    private static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/clientes/{id:int}/extrato", (int id, IDecoratedRepository repo)
            => Endpoints.GetBankStatementAsync(id, repo))
            .WithHttpLogging(Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.Response);

        app.MapPost("/clientes/{id:int}/transacoes", (
            int id,
            [FromBody] TransactionRequest transaction,
            [FromServices] IDecoratedRepository repo,
            [FromKeyedServices("NatsDestination")] string natsDestinationQueue,
            [FromServices] INatsConnection connection,
            [FromServices] Channel<int> channel,
            CancellationToken token) 
                => Endpoints.MakeTransactionAsync(id, transaction, repo, natsDestinationQueue, connection, channel, token))
                .WithHttpLogging(Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestScheme);
    }
}
