using System.Collections.Concurrent;
using System.Threading.Channels;
using NATS.Client.Core;
using Npgsql;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public sealed class SaveInBackgroundHostedService : BackgroundService
{
    private readonly NpgsqlDataSource pg;
    private readonly INatsConnection nats;
    private readonly Channel<int> channel;
    private readonly ILogger<SaveInBackgroundHostedService> logger;
    private const string BATCH_INSERT_TRANSACTION_COMMAND =
        @"INSERT INTO Transactions 
            (AccountId, Limite, Saldo, RealizadaEm, Tipo, Valor, Descricao)
        VALUES (@AccountId, @Limite, @Saldo, @RealizadaEm, @Tipo, @Valor, @Descricao);";

    public SaveInBackgroundHostedService(NpgsqlDataSource pg, INatsConnection nats, Channel<int> channel, ILogger<SaveInBackgroundHostedService> logger)
    {
        this.pg = pg ?? throw new ArgumentNullException(nameof(pg));
        this.nats = nats ?? throw new ArgumentNullException(nameof(nats));
        this.channel = channel ?? throw new ArgumentNullException(nameof(channel));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StartInThread(stoppingToken);

        return Task.CompletedTask;
    }

    private void StartInThread(CancellationToken token)
    {
        var receiveFromBusThread = new Thread(async () =>
        {
            await StartReceivingMessagesAsync(token);
        })
        {
            IsBackground = true
        };

        var saveInDatabaseThread = new Thread(async () =>
        {
            await SaveTransactionsToDatabaseAsync(token);
        })
        {
            IsBackground = true
        };

        receiveFromBusThread.Start();
        saveInDatabaseThread.Start();
    }

    private async Task SaveTransactionsToDatabaseAsync(CancellationToken token)
    {
        var start = DateTime.Now;
        var conn = await GetConnectionAsync();

        while (token.IsCancellationRequested is false)
        {
            if (transactions.IsEmpty && !channel.Reader.TryRead(out _))
                continue;

            var elapsed = DateTime.Now - start;
            if (transactions.Count >= maxQueuedMessages || elapsed >= TimeSpan.FromSeconds(3))
            {
                await SendTransactionsAsync(conn);
                start = DateTime.Now;
            }
        }

        await conn.CloseAsync();
        logger.LogInformation("Exiting saveInDatabaseThread");
    }

    const int maxQueuedMessages = 100;
    readonly ConcurrentQueue<Transaction> transactions = new ConcurrentQueue<Transaction>();

    private async Task StartReceivingMessagesAsync(CancellationToken token)
    {
        var subs = nats.SubscribeAsync<Transaction>(nameof(Transaction), cancellationToken: token);

        while (token.IsCancellationRequested is false)
        {
            if (token.IsCancellationRequested)
                break;

            await foreach (var msg in subs)
            {
                var queuedTransaction = msg.Data;
                lock (transactions)
                {
                    transactions.Enqueue(queuedTransaction);
                }
            }
        }

        logger.LogInformation("Stopped listening to Transactions queue messages.");
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
        if (transactions.IsEmpty)
        {
            logger.LogInformation("No Transactions to send.");
            return;
        }

        List<Transaction> transactionsToSave = new List<Transaction>(transactions.Count);
        lock (transactions)
        {
            while (transactions.TryDequeue(out var t))
            {
                transactionsToSave.Add(t);
            }
            
            transactions.Clear();
        }

        try
        {
            var batch = conn.CreateBatch();
            var commands = new List<NpgsqlBatchCommand>(transactionsToSave.Count);
            foreach (var transaction in transactionsToSave)
            {
                var command = new NpgsqlBatchCommand(BATCH_INSERT_TRANSACTION_COMMAND);

                command.Parameters.AddWithValue("@AccountId", NpgsqlTypes.NpgsqlDbType.Integer, transaction.AccountId);
                command.Parameters.AddWithValue("@Limite", NpgsqlTypes.NpgsqlDbType.Numeric, transaction.Limite);
                command.Parameters.AddWithValue("@Saldo", NpgsqlTypes.NpgsqlDbType.Numeric, transaction.Saldo);
                command.Parameters.AddWithValue("@Tipo", NpgsqlTypes.NpgsqlDbType.Varchar, transaction.Tipo);
                command.Parameters.AddWithValue("@Valor", NpgsqlTypes.NpgsqlDbType.Numeric, transaction.Valor);
                command.Parameters.AddWithValue("@RealizadaEm", NpgsqlTypes.NpgsqlDbType.Timestamp, transaction.RealizadaEm);
                command.Parameters.AddWithValue("@Descricao", NpgsqlTypes.NpgsqlDbType.Varchar, transaction.Descricao);

                batch.BatchCommands.Add(command);
            }

            await batch.ExecuteNonQueryAsync();
            transactions.Clear();
            logger.LogInformation("Executed Batch Saving in database for {transactionsToSaveCount} transactions.", 
                transactionsToSave.Count);
        }
        catch (Exception ex)
        {
            logger.LogError("Exception happened saving batch to database {exception}", ex.Message);
        }
    }
}