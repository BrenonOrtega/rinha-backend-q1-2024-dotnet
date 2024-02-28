using System.Security.Cryptography;
using System.Text.Json;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using NATS.Client.JetStream;
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

        if (accountValues.Length == 0 || Array.TrueForAll(accountValues, x => x.Name.IsNullOrEmpty))
        {
            return await QueryAndSetCache(db, id);
        }

        int limite = 0;
        int saldo = 0;

        foreach (var hash in accountValues)
        {
            _ = (string)hash.Name switch
            {
                Limite => limite = int.Parse(hash.Value),
                Saldo => saldo = int.Parse(hash.Value),
                _ => 0
            };
        }

        return new Account(id, limite, saldo);
    }

    private async Task<Account> QueryAndSetCache(IDatabase db, int id)
    {
        var account = await next.GetAccountByIdAsync(id);

        if (account is not null)
            await SetCacheAsync(db, account.Id, account.Limite, account.Saldo);

        return account;
    }

    private static async Task SetCacheAsync(IDatabaseAsync db, int id, int limite, int saldo)
    {
        string key = GetAccountKey(id);
        await db.HashSetAsync(key: key, [
            new HashEntry(Limite, limite),
            new HashEntry(Saldo, saldo)
        ]);
    }

    private static string GetAccountKey(int id) => $"{AccountHashPrefix}{id}";

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

    public async Task<bool> Save(Transaction transaction)
    {
        var db = multiplexer.GetDatabase();

        var updateSnapshot = UpdateAccountSnapshot(transaction, db);
        var setBankStatement = SetBankStatementAsync(db, transaction);
        await Task.WhenAll(updateSnapshot, setBankStatement);

        return updateSnapshot.Result;
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

    private Task<bool> UpdateAccountSnapshot(Transaction transaction, IDatabase db)
    {
        Task<bool> updateSnapshot;
        if (transaction.Tipo is Transaction.Credit)
            updateSnapshot = IncrementAcountValue(db, transaction);
        else
        {
            updateSnapshot = TryDecrementAccountValue(db, transaction);
        }

        return updateSnapshot;
    }

    private static async Task<bool> IncrementAcountValue(IDatabaseAsync db, Transaction transaction)
    {
        long newSaldo = default;

        var key = GetAccountKey(transaction.AccountId);
        async Task<long> IncrementSaldoAsync() 
            => newSaldo = await db.HashIncrementAsync(
                    key: key,
                    hashField: Saldo,
                    value: transaction.Valor);

        await Task.WhenAll(IncrementSaldoAsync(), UpdateLimiteAsync(db, key, transaction));

        transaction.UpdateSaldo(newSaldo);
        return true;
    }

    private static async Task UpdateLimiteAsync(IDatabaseAsync db, string accountId, Transaction transaction)
    {
        await db.HashSetAsync(accountId, [new HashEntry(Limite, transaction.Limite)]);
    }

    private async Task<bool> TryDecrementAccountValue(IDatabase db, Transaction transaction)
    {
        int accountId = transaction.AccountId;
        var account = await GetAccountByIdCore(accountId, db);
        int value = transaction.Valor;
        if (account is not null && account.CanExecuteDebt(value))
        {
            var key = GetAccountKey(accountId);
            long newSaldo = default;
            async Task DecrementAsync() => newSaldo = await db.HashDecrementAsync(key, Saldo, value);

            await Task.WhenAll(
                DecrementAsync(), 
                UpdateLimiteAsync(db, key, transaction));

            Console.WriteLine($"Expected saldo for transaction was {transaction.Saldo} and was {newSaldo}");
            transaction.UpdateSaldo(newSaldo);

            return true;
        }

        return false;
    }
}