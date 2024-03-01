using System.Security.Cryptography;
using System.Text.Json;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using NATS.Client.JetStream;
using StackExchange.Redis;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Services;

public sealed class RedisTransactionService : ITransactionService
{
    private const string Limite = nameof(Account.Limite);
    private const string Saldo = nameof(Account.Saldo);
    private const string AccountHashPrefix = "Account:";
    private const string BankStatementPrefix = "BankStatement:";

    private readonly ConnectionMultiplexer multiplexer;
    ConnectionMultiplexer multiplexer,
    private readonly string natsDestinationQueue;
    private readonly INatsConnection connection;
    private readonly Channel<int> channel;

    private static readonly JsonSerializerOptions options;

    public CacheRepository(
        ConnectionMultiplexer multiplexer,
        [FromKeyedServices("NatsDestination")] string natsDestinationQueue,
        [FromServices] INatsConnection connection,
        Channel<int> channel)
    {
        this.multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));
        this.natsDestinationQueue = natsDestinationQueue ?? throw new ArgumentNullException(nameof(natsDestinationQueue));
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection)); ;
        this.channel = channel ?? throw new ArgumentNullException(nameof(channel)); ;
    }

    static CacheRepository()
    {
        options = new JsonSerializerOptions();
        options.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    }

    public async Task<ExecuteTransactionResponse> TryExecuteTransactionAsync(int accountId, TransactionRequest transaction)
    {
        var db = multiplexer.GetDatabase();
        var account = await GetAccountByIdCore(accountId, db);

        if (account is null || account.IsEmpty())
            return new ExecuteTransactionResponse(false, -1, -1, TransactionExecutionError.AccountNotFound);

        if (account.CanExecute(transaction) is false)
            return new ExecuteTransactionResponse(false, -1, -1, TransactionExecutionError.ExceedLimitError);

        var createdTransaction = account.Execute(transaction);

        var key = GetBankStatementKey(accountId);
        await InsertTransactionAsync(db, key, createdTransaction);

        await Task.WhenAll(
            db.SortedSetRemoveRangeByRankAsync(key, 0, -11),
            connection.PublishAsync<Transaction>(
                natsDestinationQueue,
                createdTransaction,
                cancellationToken: token));

        channel.Writer.TryWrite(default);

        return new ExecuteTransactionResponse(true, createdTransaction.Limite, createdTransaction.Saldo);
    }

    private async Task<Account> GetAccountByIdCore(int id, IDatabase db)
    {
        var transactions = await db.SortedSetRangeByRankAsync(GetBankStatementKey(id), -1, -1, Order.Descending);

        string lastTransaction = transactions.FirstOrDefault();
        if (string.IsNullOrEmpty(lastTransaction))
            return null;

        var t = JsonSerializer.Deserialize<Transaction>(lastTransaction, options);

        return new Account(t.AccountId, t.Limite, t.Saldo);
    }

    private static async Task InsertTransactionAsync(IDatabaseAsync db, string key, Transaction transaction)
    {
        var serialized = JsonSerializer.Serialize(transaction, options);

        await db.SortedSetAddAsync(key, serialized, transaction.RealizadaEm.Ticks);
    }

    public async Task<BankStatement> GetBankStatementAsync(int id)
    {
        var db = multiplexer.GetDatabase();

        var account = await GetAccountByIdCore(id, db);

        if (account is null || account.IsEmpty())
            return new BankStatement();

        var transactions = GetLastTransactionsAsync(id, db);

        return new BankStatement(
            saldo: new Balance(account.Saldo, account.Limite),
            ultimasTransacoes: transactions);
    }

    public async Task<List<BankStatementTransaction>> GetLastTransactionsAsync(int id, IDatabaseAsync db)
    {
        var jsonValues = await db.SortedSetRangeByRankAsync(GetBankStatementKey(id), -1, 0, Order.Descending);

        List<BankStatementTransaction> transactions = new(jsonValues.Length);
        foreach (string x in jsonValues)
        {
            var t = JsonSerializer.Deserialize<BankStatementTransaction>(x, options);
            transactions.Add(t);
        }

        return transactions;
    }

    private static string GetBankStatementKey(int id) => $"{BankStatementPrefix}{id}";
}