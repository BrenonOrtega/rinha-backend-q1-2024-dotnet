using System.Diagnostics.Metrics;
using System.Timers;
using NATS.Client.Core;
using NATS.Client.Serializers.Json;
using Npgsql;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public sealed class SaveInBackgroundHostedService : BackgroundService
{
    private readonly NpgsqlDataSource pg;
    private readonly INatsConnection nats;
    private const string BATCH_INSERT_TRANSACTION_COMMAND =
        @"INSERT INTO Transactions 
            (AccountId, Limite, Saldo, RealizadaEm, Tipo, Valor)
        VALUES (@AccountId, @Limite, @Saldo, @RealizadaEm, @Tipo, @Valor);";

    public SaveInBackgroundHostedService(NpgsqlDataSource pg, INatsConnection nats)
    {
        this.pg = pg ?? throw new ArgumentNullException(nameof(pg));
        this.nats = nats ?? throw new ArgumentNullException(nameof(nats));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //var thread = StartInThread(stoppingToken);

        while (true)
        {
            await Execute(stoppingToken);

            await Task.Delay(1000, stoppingToken);
        }
    }

    private Thread StartInThread(CancellationToken token)
    {
        var thread = new Thread(async () =>
        {
            using NpgsqlConnection conn = await Execute(token);
        })
        {
            IsBackground = true
        }; ;

        thread.Start();

        return thread;
    }

    private bool shouldSendMessages = false;
    private void MarkToSendMessages(object _, ElapsedEventArgs __) => shouldSendMessages = true;
    const int maxQueuedMessages = 100;
    readonly List<Transaction> transactions = new(maxQueuedMessages);

    private async Task<NpgsqlConnection> Execute(CancellationToken token)
    {
        using NpgsqlConnection conn = await GetConnectionAsync();
        var subs = nats.SubscribeAsync<Transaction>(nameof(Transaction));

        var start = DateTime.Now;
        while (true)
        {
            if (token.IsCancellationRequested)
                break;

            await foreach (var msg in subs)
            {
                var queuedTransaction = msg.Data;
                if (transactions.Count <= maxQueuedMessages)
                {
                    transactions.Add(queuedTransaction);
                }

                var elapsed = DateTime.Now - start;
                if (transactions.Count >= maxQueuedMessages || elapsed >= TimeSpan.FromSeconds(3))
                {
                    await SendTransactionsAsync(conn);
                    start = DateTime.Now;
                }
            }
        }

        await conn.CloseAsync();
        return conn;
    }

    private async Task<NpgsqlConnection> GetConnectionAsync()
    {
        try
        {
            return await pg.OpenConnectionAsync();
        }
        catch
        {
            await Task.Delay(150);
            return await GetConnectionAsync();
        }
    }

    private async Task SendTransactionsAsync(NpgsqlConnection conn)
    {
        if (transactions.Count == 0)
        {
            Console.WriteLine("No Transactions to send.");
            return;
        }

        Transaction[] transactionsToSave = new Transaction[transactions.Count];
        transactions.CopyTo(transactionsToSave);

        try
        {
            var batch = conn.CreateBatch();
            var commands = new List<NpgsqlBatchCommand>(transactionsToSave.Length);
            foreach (var transaction in transactionsToSave)
            {
                var command = new NpgsqlBatchCommand(BATCH_INSERT_TRANSACTION_COMMAND);

                command.Parameters.AddWithValue("@AccountId", NpgsqlTypes.NpgsqlDbType.Integer, transaction.AccountId);
                command.Parameters.AddWithValue("@Limite", NpgsqlTypes.NpgsqlDbType.Numeric, transaction.Limite);
                command.Parameters.AddWithValue("@Saldo", NpgsqlTypes.NpgsqlDbType.Numeric, transaction.Saldo);
                command.Parameters.AddWithValue("@Tipo", NpgsqlTypes.NpgsqlDbType.Varchar, transaction.Tipo);
                command.Parameters.AddWithValue("@Valor", NpgsqlTypes.NpgsqlDbType.Numeric, transaction.Valor);
                command.Parameters.AddWithValue("@RealizadaEm", NpgsqlTypes.NpgsqlDbType.Date, transaction.RealizadaEm);
                command.Parameters.AddWithValue("@Descricao", NpgsqlTypes.NpgsqlDbType.Varchar, transaction.Descricao);

                batch.BatchCommands.Add(command);
            }

            await batch.ExecuteNonQueryAsync();
            transactions.Clear();
            Console.WriteLine($"Executed Batch Saving in database for {transactionsToSave.Length} transactions.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}