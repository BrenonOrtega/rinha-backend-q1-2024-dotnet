using System.Collections.Concurrent;
using System.Text.Json;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using Microsoft.AspNetCore.Mvc;
using NATS.Client.Core;
using ProtoBuf;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

internal static class Program
{
    private static readonly IResult NotFoundResponse = Results.NotFound();
    private static readonly IResult UnprocessableEntityResponse = Results.UnprocessableEntity();
    private static readonly IResult EmptyOkResponse = Results.Ok();

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

        builder.Services.ConfigureInfrastructure(
            builder.Configuration.GetConnectionString("Postgres"));

        builder.Services.ConfigureMessaging(builder.Configuration);

        builder.Services.ConfigureBackgroundServices();

        return builder;
    }

    private static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/clientes/{id:int}/extrato",
            async (int id, [FromServices] IDecoratedRepository repo) =>
            {
                var bankStatement = await repo.GetBankStatementAsync(id);

                if (bankStatement is not null && !bankStatement.IsEmpty())
                    return Results.Ok(bankStatement);

                return NotFoundResponse;
            }).WithHttpLogging(Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.Response);

        app.MapPost("/clientes/{id:int}/transacoes", async (
            int id,
            [FromBody] TransactionRequest transaction,
            [FromServices] IDecoratedRepository repo,
            [FromServices] ConcurrentQueue<Transaction> queue,
            [FromKeyedServices("NatsDestination")] string natsDestinationQueue,
            [FromServices] INatsConnection connection,
            CancellationToken token) =>
        {
            if (transaction.IsInvalid())
                return UnprocessableEntityResponse;

            var account = await repo.GetAccountByIdAsync(id);

            if (account.IsEmpty())
                return NotFoundResponse;

            if (account.CanExecute(transaction) is false)
                return UnprocessableEntityResponse;

            var createdTransaction = account.Execute(transaction);

            await repo.Save(createdTransaction);

            await connection.PublishAsync(natsDestinationQueue, account, cancellationToken: token);

            queue.Enqueue(createdTransaction);

            return EmptyOkResponse;
        }).WithHttpLogging(Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestScheme);
    }
}
