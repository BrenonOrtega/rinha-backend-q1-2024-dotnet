
using System.Collections.Concurrent;
using System.Threading.Channels;
using Npgsql;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public sealed class SaveInBackgroundHostedService : BackgroundService
{
    private readonly NpgsqlDataSource pg;
    private readonly ConcurrentQueue<Transaction> queue;
    private const string BATCH_INSERT_TRANSACTION_COMMAND =
        @"INSERT INTO Transactions 
            (AccountId, Limite, Saldo, RealizadaEm, Tipo, Valor)
        VALUES (@AccountId, @Limite, @Saldo, @RealizadaEm, @Tipo, @Valor);";

    public SaveInBackgroundHostedService(NpgsqlDataSource pg, ConcurrentQueue<Transaction> queue)
    {
        this.pg = pg ?? throw new ArgumentNullException(nameof(pg));
        this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var thread = StartInThread(stoppingToken);

        while (true)
        {
            if (stoppingToken.IsCancellationRequested)
                thread.Join();
            
            await Task.Delay(1000,stoppingToken);
        }
    }

    private Thread StartInThread(CancellationToken token)
    {
        var thread = new Thread(async () =>
        {
            var connOpened = false;
            using var conn = pg.CreateConnection();
            const int maxQueuedMessages = 100;
            var transactions = new List<Transaction>(maxQueuedMessages);

            while (true)
            {
                if (token.IsCancellationRequested)
                    break;

                while (queue.TryDequeue(out var queuedTransaction) && transactions.Count <= maxQueuedMessages)
                {
                    transactions.Add(queuedTransaction);
                }

                if (transactions.Count <= maxQueuedMessages)
                    continue;
                
                if (connOpened is false)
                {
                    await conn.OpenAsync();
                    connOpened = true;
                }

                try
                {
                    var batch = conn.CreateBatch();
                    var commands = new List<NpgsqlBatchCommand>(queue.Count);
                    foreach (var transaction in transactions)
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

            await conn.CloseAsync();
        })
        {
            IsBackground = true
        };;

        thread.Start();

        return thread;
    }
}