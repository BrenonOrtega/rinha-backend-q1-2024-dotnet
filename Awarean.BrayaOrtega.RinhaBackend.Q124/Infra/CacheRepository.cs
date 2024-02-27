using System.Text.Json;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using StackExchange.Redis;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;

public sealed class CacheRepository : IDecoratedRepository
{
    private const string Limite = nameof(Account.Limite);
    private const string Saldo = nameof(Account.Saldo);
    private const string AccountHashPrefix = "Account:";
    private const string BankStatementPrefix = "BankStatement:";

    private readonly ConnectionMultiplexer multiplexer;
    private readonly IRepository next;
    private static readonly JsonSerializerOptions options;

    public CacheRepository(ConnectionMultiplexer multiplexer, IRepository next)
    {
        this.multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));
        this.next = next ?? throw new ArgumentNullException(nameof(next));
    }

    static CacheRepository()
    {
        options = new JsonSerializerOptions();
        options.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    }

    public async Task<Account> GetAccountByIdAsync(int id)
    {
        var db = multiplexer.GetDatabase();
        return await GetAccountByIdCore(id, db);
    }

    private async Task<Account> GetAccountByIdCore(int id, IDatabase db)
    {
        var accountValues = await db.HashGetAllAsync($"{AccountHashPrefix}{id}");

        if (accountValues.Length == 0 || Array.TrueForAll(accountValues, x => x.Value.IsNullOrEmpty))
        {
            return await QueryAndSetCache(db, id);
        }

        int limite = -1;
        int saldo = -1;

        foreach (var hash in accountValues)
        {
            if (hash.Name == Limite)
                limite = int.Parse(hash.Value);
            if (hash.Name == Saldo)
                saldo = int.Parse(hash.Value);
        }

        if (limite is -1 && saldo is -1)
            return await QueryAndSetCache(db, id);

        return new Account(id, limite, saldo);
    }

    private async Task<Account> QueryAndSetCache(IDatabase db, int id)
    {
        var account = await next.GetAccountByIdAsync(id);

        await SetCacheAsync(db, account.Id, account.Limite, account.Saldo);

        return account;
    }

    private static async Task SetCacheAsync(IDatabaseAsync db, int id, int limite, int saldo)
    {
        await db.HashSetAsync($"{AccountHashPrefix}{id}", [
            new HashEntry(Limite, limite),
            new HashEntry(Saldo, saldo)
        ]);
    }

    public async Task<BankStatement> GetBankStatementAsync(int id)
    {
        var db = multiplexer.GetDatabase();

        var account = await GetAccountByIdCore(id, db);

        if (account is null || account.IsEmpty())
            return new BankStatement();

        var jsonValues = await db.SortedSetRangeByRankAsync(GetBankStatementKey(id), 0, -1, Order.Descending);

        List<BankStatementTransaction> transactions = new(jsonValues.Length);
        foreach (string x in jsonValues)
        {
            var t = JsonSerializer.Deserialize<BankStatementTransaction>(x, options);
            transactions.Add(t);
        }

        return new BankStatement(
            saldo: new Balance(account.Saldo, account.Limite),
            ultimasTransacoes: transactions);
    }

    private static string GetBankStatementKey(int id) => $"{BankStatementPrefix}{id}";

    public async Task Save(Transaction transaction)
    {
        var db = multiplexer.GetDatabase();
        var task1 = SetCacheAsync(db, transaction.AccountId, transaction.Limite, transaction.Saldo);
        var task2 = SetBankStatementAsync(db, transaction);
        await Task.WhenAll(task1, task2);
    }

    private static async Task SetBankStatementAsync(IDatabaseAsync db, Transaction transaction)
    {
        var key = GetBankStatementKey(transaction.AccountId);
        var BankStatementTransaction = new BankStatementTransaction(
            valor: transaction.Valor,
            tipo: transaction.Tipo,
            descricao: transaction.Descricao,
            realizadaEm: transaction.RealizadaEm);
            
        var serialized = JsonSerializer.Serialize(BankStatementTransaction, options);

        await db.SortedSetAddAsync(key, serialized, transaction.RealizadaEm.Ticks);
        await db.SortedSetRemoveRangeByRankAsync(key, 0, -11);
    }
}