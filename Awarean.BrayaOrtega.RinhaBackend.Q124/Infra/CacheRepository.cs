using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using StackExchange.Redis;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;

public sealed class CacheRepository : IDecoratedRepository
{
    private readonly ConnectionMultiplexer multiplexer;
    private readonly IRepository next;

    public CacheRepository(ConnectionMultiplexer multiplexer, IRepository next)
    {
        this.multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));
        this.next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task<Account> GetAccountByIdAsync(int id)
    {
        var db = multiplexer.GetDatabase();

        var accountValues = db.HashGetAll($"{AccountHashPrefix}{id}");

        if (accountValues.Length == 0 || Array.TrueForAll(accountValues, x => x.Value.IsNullOrEmpty))
        {
            return await QueryAndSetCache(db, id);
        }

        long limite = -1;
        long saldo = -1;

        foreach (var hash in accountValues)
        {
            if (hash.Name == Limite)
                limite = long.Parse(hash.Value);
            if (hash.Name == Saldo)
                saldo = long.Parse(hash.Value);
        }

        if (limite is -1 && saldo is -1)
            return await QueryAndSetCache(db, id);

        return new Account(id, limite, saldo);
    }

    private const string Limite = nameof(Account.Limite);
    private const string Saldo = nameof(Account.Saldo);
    private const string AccountHashPrefix = "Account:";

    private async Task<Account> QueryAndSetCache(IDatabase db, int id)
    {
        var account = await next.GetAccountByIdAsync(id);
        await SetCacheAsync(db, account);

        return account;
    }

    private static async Task SetCacheAsync(IDatabase db, Account account)
    {
        await db.HashSetAsync($"{AccountHashPrefix}{account.Id}", [
            new HashEntry(Limite, account.Limite),
            new HashEntry(Saldo, account.Saldo)
        ]);
    }

    public async Task<BankStatement> GetBankStatementAsync(int id)
    {
        return await next.GetBankStatementAsync(id);
    }

    public async Task Save(Account account, Transaction transaction)
    {
        var db = multiplexer.GetDatabase();
        await SetCacheAsync(db, account);
        //await SetCacheAsync(db, transaction);
    }
}
