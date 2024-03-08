using System.Text.Json;
using System.Threading.Channels;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using NATS.Client.Core;
using StackExchange.Redis;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;

public sealed class CachedRepository : ICachedRepository
{
    private const string BankStatementPrefix = "BankStatement:";

    private readonly ConnectionMultiplexer multiplexer;

    private static readonly JsonSerializerOptions options;

    public CachedRepository(ConnectionMultiplexer multiplexer)
    {
        this.multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));
    }

    static CachedRepository()
    {
        options = new JsonSerializerOptions();
        options.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    }

    public async Task<Account> GetAccountByIdAsync(int id)
    {
        var db = multiplexer.GetDatabase();
        var account = await GetAccountByIdCore(id, db).ConfigureAwait(false);

        return account;
    }

    private static async Task<Account> GetAccountByIdCore(int id, IDatabase db)
    {
        var transactions = db.SortedSetRangeByRankWithScores(GetBankStatementKey(id), order: Order.Descending, start:0, stop: 1);

        var lastTransaction = transactions.FirstOrDefault();
        if (string.IsNullOrEmpty(lastTransaction.Element))
            return null;

        string stringTransaction = lastTransaction.Element;
        var t = JsonSerializer.Deserialize<Transaction>(stringTransaction, options);

        return new Account(t.AccountId, t.Limite, t.Saldo);
    }

    private static string GetBankStatementKey(int id) => $"{BankStatementPrefix}{id}";

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
        var key = GetBankStatementKey(id);
        var jsonValues = await db.SortedSetRangeByRankAsync(
            key: key,
            start: -11,
            stop: -1,
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
                
                if (b.Descricao is null or "")
                    continue;
            }

            var t = JsonSerializer.Deserialize<BankStatementTransaction>(x, options);
            transactions.Add(t);
        }

        return (balance, transactions);
    }

    public async Task<bool> Save(Transaction transaction)
    {
        var db = multiplexer.GetDatabase();
        var key = GetBankStatementKey(transaction.AccountId);

        await InsertTransactionAsync(db, key, transaction).ConfigureAwait(false);

        await db.SortedSetRemoveRangeByRankAsync(key, 0, -11).ConfigureAwait(false);

        return true;
    }

    private static async Task InsertTransactionAsync(IDatabaseAsync db, string key, Transaction transaction)
    {
        var serialized = JsonSerializer.Serialize(transaction, options);

        await db.SortedSetAddAsync(key, serialized, transaction.RealizadaEm.Ticks);
    }   
}
