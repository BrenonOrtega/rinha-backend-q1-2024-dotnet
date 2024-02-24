using System.Collections.Concurrent;
using System.Text.Json;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;

public sealed class CacheRepository : IDecoratedRepository
{
    private readonly ConcurrentDictionary<int, Account> cache;
    private readonly IRepository next;
    private static readonly JsonSerializerOptions options;

    public CacheRepository(ConcurrentDictionary<int, Account> cache, IRepository next)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.next = next ?? throw new ArgumentNullException(nameof(next));
    }

    static CacheRepository()
    {
        options = new JsonSerializerOptions();
        options.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    }

    public async Task<Account> GetAccountByIdAsync(int id)
    {
        var exists = cache.TryGetValue(id, out var account);

        if (exists is false)
            return await QueryAndSetCache(id);

        return account;
    }

    private async Task<Account> QueryAndSetCache(int id)
    {
        var account = await next.GetAccountByIdAsync(id);
        await SetCacheAsync(account.Id, account.Limite, account.Saldo);

        return account;
    }

    private Task SetCacheAsync(int id, int limite, int saldo)
    {
        var account = new Account(id, limite, saldo);
        cache.AddOrUpdate(id, account, (id, existing) =>
        {
            return account;
        });

        return Task.CompletedTask;
    }

    static readonly DistributedCacheEntryOptions cacheOptions = new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(50) };
    public async Task<BankStatement> GetBankStatementAsync(int id)
    {
        var bankStatement = await next.GetBankStatementAsync(id);

        return bankStatement;
    }

    public async Task Save(Transaction transaction)
    {
        await SetCacheAsync(transaction.AccountId, transaction.Limite, transaction.Saldo);
    }
}
