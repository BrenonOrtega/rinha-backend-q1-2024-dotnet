using System.Text.Json;
using System.Threading.Channels;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using NATS.Client.Core;
using StackExchange.Redis;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Services;

public sealed class RedisTransactionService : ITransactionService
{
    private const string BankStatementPrefix = "BankStatement:";

    private readonly ConnectionMultiplexer multiplexer;
    private readonly string natsDestinationQueue;
    private readonly INatsConnection connection;
    private readonly Channel<int> channel;

    private static readonly JsonSerializerOptions options;

    public RedisTransactionService(
        ConnectionMultiplexer multiplexer,
        [FromKeyedServices("NatsDestination")] string natsDestinationQueue,
        INatsConnection connection,
        Channel<int> channel)
    {
        this.multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));
        this.natsDestinationQueue = natsDestinationQueue ?? throw new ArgumentNullException(nameof(natsDestinationQueue));
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection)); ;
        this.channel = channel ?? throw new ArgumentNullException(nameof(channel)); ;
    }

    static RedisTransactionService()
    {
        options = new JsonSerializerOptions();
        options.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    }

    public async Task<ExecuteTransactionResponse> TryExecuteTransactionAsync(
        int accountId, TransactionRequest transaction, CancellationToken token)
    {
        var db = multiplexer.GetDatabase();
        var account = await GetAccountByIdCore(accountId, db).ConfigureAwait(false);

        if (account is null || account.IsEmpty())
            return new ExecuteTransactionResponse(false, -1, -1, TransactionExecutionError.AccountNotFound);

        if (account.CanExecute(transaction) is false)
            return new ExecuteTransactionResponse(false, -1, -1, TransactionExecutionError.ExceedLimitError);

        var createdTransaction = account.Execute(transaction);

        var key = GetBankStatementKey(accountId);

        await InsertTransactionAsync(db, key, createdTransaction).ConfigureAwait(false);

        await db.SortedSetRemoveRangeByRankAsync(key, 0, -11).ConfigureAwait(false);
        await connection.PublishAsync<Transaction>(
                natsDestinationQueue,
                createdTransaction,
                cancellationToken: token).ConfigureAwait(false);

        channel.Writer.TryWrite(default);

        return new ExecuteTransactionResponse(true, createdTransaction.Limite, createdTransaction.Saldo);
    }

    private static async Task<Account> GetAccountByIdCore(int id, IDatabase db)
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

       var (balance, transactions) = await GetLastTransactionsAsync(id, db).ConfigureAwait(false);

        if (balance is null || balance.IsEmpty())
            return new BankStatement();

        return new BankStatement(
            saldo: balance,
            ultimasTransacoes: transactions);
    }

    public async Task<(Balance, List<BankStatementTransaction>)> GetLastTransactionsAsync(int id, IDatabaseAsync db)
    {
        var jsonValues = await db.SortedSetRangeByRankAsync(
            key: GetBankStatementKey(id),
            start: -1,
            stop: 0,
            order: Order.Descending)
            .ConfigureAwait(false);

        Balance balance = null;
        List<BankStatementTransaction> transactions = new(jsonValues.Length);

        for(var index =0; index < jsonValues.Length; index++) 
        {
            string x = jsonValues[index];
            if (index == 0)
            {
                var b = JsonSerializer.Deserialize<Transaction>(x, options);
                balance = new Balance(b.Saldo, b.Limite);
            }

            var t = JsonSerializer.Deserialize<BankStatementTransaction>(x, options);
            transactions.Add(t);
        }

        return (balance, transactions);
    }

    private static string GetBankStatementKey(int id) => $"{BankStatementPrefix}{id}";
}