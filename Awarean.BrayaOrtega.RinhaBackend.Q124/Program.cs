using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

internal static class Program
{
    private const string BATCH_INSERT_TRANSACTION_COMMAND = @"INSERT INTO Transactions 
                                    (AccountId, Limite, Saldo, RealizadaEm, Tipo, Valor)
                                VALUES (@AccountId, @Limite, @Saldo, now(), @Tipo, @Valor);";

    private static readonly IResult NotFoundResponse = Results.NotFound();
    private static readonly IResult UnprocessableEntityResponse = Results.UnprocessableEntity();
    private static readonly IResult EmptyOkResponse = Results.Ok();

    public static void Main(string[] args)
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

                if (bankStatement is not null && !bankStatement.IsEmpty())
                    return Results.Ok(bankStatement);

                return NotFoundResponse;
            });

        app.MapPost("/clientes/{id:int}/transacoes", async (
            int id,
            [FromBody] TransactionRequest transaction,
            [FromServices] IDecoratedRepository repo,
            [FromServices] Channel<int> channel,
            [FromServices] ConcurrentQueue<Transaction> queue,
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
            
            queue.Enqueue(createdTransaction);

            // Could be any number, just signals thread to process.
            channel.Writer.TryWrite(1);

            return EmptyOkResponse;
        });
    }

    private static WebApplicationBuilder ConfigureServices(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        builder.Services.AddLogging();
        builder.Services.ConfigureInfrastructure(
            builder.Configuration.GetConnectionString("Postgres"),
            builder.Configuration.GetConnectionString("Redis"));

        return builder;
    }

    private static void ConfigureBackgroundServices(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(_ => Channel.CreateUnbounded<int>());

        builder.Services.AddSingleton<Repository>((p) => new Repository(p.GetRequiredService<NpgsqlDataSource>()));
    }

    private static void StartBackgroundServices(WebApplication app)
    {
        var thread = new Thread(async () =>
        {
            var scope = app.Services.CreateScope();

            var channel = scope.ServiceProvider.GetRequiredService<Channel<int>>();
            var pg = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
            var queue = scope.ServiceProvider.GetRequiredService<ConcurrentQueue<Transaction>>();

            var connOpened = false;
            using var conn = pg.CreateConnection();
            const int maxQueuedMessages = 100;
            var transactions = new List<Transaction>(maxQueuedMessages);

            while (true)
            {
                if (channel.Reader.TryRead(out var _))
                {
                    if (connOpened is false)
                    {
                        await conn.OpenAsync();
                        connOpened = true;
                    }

                    while (queue.TryDequeue(out var queuedTransaction) && transactions.Count <= maxQueuedMessages)
                    {
                        transactions.Add(queuedTransaction);
                    }

                    if (transactions.Count <= maxQueuedMessages)
                        continue;

                    try
                    {
                        var batch = conn.CreateBatch();
                        var commands = new List<NpgsqlBatchCommand>(queue.Count);
                        foreach(var transaction in transactions)
                        {
                            var command = new NpgsqlBatchCommand(BATCH_INSERT_TRANSACTION_COMMAND);

                            command.Parameters.AddWithValue("@AccountId", NpgsqlTypes.NpgsqlDbType.Integer, transaction.AccountId);
                            command.Parameters.AddWithValue("@Limite", NpgsqlTypes.NpgsqlDbType.Numeric, transaction.Limite);
                            command.Parameters.AddWithValue("@Saldo", NpgsqlTypes.NpgsqlDbType.Numeric, transaction.Saldo);
                            command.Parameters.AddWithValue("@Tipo", NpgsqlTypes.NpgsqlDbType.Varchar, transaction.Tipo);
                            command.Parameters.AddWithValue("@Valor", NpgsqlTypes.NpgsqlDbType.Numeric, transaction.Valor);
                            command.Parameters.AddWithValue("@RealizadaEm", NpgsqlTypes.NpgsqlDbType.Date, transaction.RealizadaEm);

                            batch.BatchCommands.Add(command);
                        }

                        await batch.ExecuteNonQueryAsync();
                        transactions.Clear();
                        Console.WriteLine($"Executed Batch Saving in database for {maxQueuedMessages} transactions.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        });

        thread.Start();
    }
}
