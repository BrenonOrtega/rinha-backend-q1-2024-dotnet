using System.Threading.Channels;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

internal static class Program
{
    private static readonly IResult NotFoundResponse = Results.NotFound();
    private static readonly IResult UnprocessableEntityResponse = Results.UnprocessableEntity();
    private static readonly IResult EmptyOkResponse = Results.Ok();

    private static void Main(string[] args)
    {
        WebApplicationBuilder builder = ConfigureServices(args);

        ConfigureBackgroundServices(builder);

        var app = builder.Build();

        StartBackgroundServices(app);

        MapEndpoints(app);

        app.Run();
    }


    private static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/clientes/{id:int}/extrato",
            async (int id, [FromServices] IDecoratedRepository repo) =>
            {
                var bankStatement = await repo.GetBankStatementAsync(id);

                if (!bankStatement.IsEmpty())
                    return Results.Ok(bankStatement);

                return NotFoundResponse;
            })
            .CacheOutput(x => x.VaryByValue(varyBy: httpContext => new KeyValuePair<string, string>("id", httpContext.Request.RouteValues["id"].ToString()))); ;

        app.MapPost("/clientes/{id:int}/transacoes", async (
            int id,
            [FromBody] TransactionRequest transaction,
            [FromServices] IDecoratedRepository repo,
            [FromServices] Channel<UpdateRequest> channel,
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

            var request = new UpdateRequest(account, createdTransaction, token);

            await repo.Save(request.Account, request.CreatedTransaction);

            await channel.Writer.WriteAsync(request, token);

            return EmptyOkResponse;
        });
    }

    private static WebApplicationBuilder ConfigureServices(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        builder.Services.AddLogging();
        builder.Services.ConfigureInfrastructure(builder.Configuration.GetConnectionString("Postgres"),
            builder.Configuration.GetConnectionString("Redis"));

        return builder;
    }

    private static void ConfigureBackgroundServices(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(_ => Channel.CreateUnbounded<UpdateRequest>());

        builder.Services.AddSingleton<Repository>((p) => new Repository(p.GetRequiredService<NpgsqlDataSource>()));
    }

    private static void StartBackgroundServices(WebApplication app)
    {
        var thread = new Thread(async () =>
        {
            var scope = app.Services.CreateScope();

            var channel = scope.ServiceProvider.GetRequiredService<Channel<UpdateRequest>>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository>();

            while (true)
            {
                if (channel.Reader.TryRead(out var updateRequest))
                {
                    if (updateRequest.Token.IsCancellationRequested)
                        continue;

                    try
                    {
                        await repo.Save(updateRequest.Account, updateRequest.CreatedTransaction);
                    }
                    catch (Exception ex)
                    {
                        await channel.Writer.WriteAsync(updateRequest, updateRequest.Token);
                        Console.WriteLine(ex.Message);
                    }
                }

                await Task.Delay(3);
            }
        });

        thread.Start();
    }
}
