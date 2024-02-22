using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;

public sealed class CacheRepository : IDecoratedRepository
{
    private readonly IDistributedCache cache;
    private readonly IRepository next;
    private static readonly JsonSerializerOptions options;

    public CacheRepository(IDistributedCache cache, IRepository next)
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
        var saldo = -1;
        var limite = -1;
        var tasks = new List<Func<Task>>(){
            async () => {

                var bytes = await cache.GetAsync($"{AccountHashPrefix}{id}:{Saldo}");
                saldo = BitConverter.ToInt32(bytes);
            },
            async () =>{
                var bytes = await cache.GetAsync($"{AccountHashPrefix}{id}:{Limite}");
                limite = BitConverter.ToInt32(bytes);
            }
        }.Select(x => x());

        await Task.WhenAll(tasks);

        if (saldo == -1 && limite == -1)
            return await QueryAndSetCache(id);

        return new Account(id, limite, saldo);
    }

    private const string Limite = nameof(Account.Limite);
    private const string Saldo = nameof(Account.Saldo);
    private const string AccountHashPrefix = "Account:";

    private async Task<Account> QueryAndSetCache(int id)
    {
        var account = await next.GetAccountByIdAsync(id);
        await SetCacheAsync(account.Id, account.Limite, account.Saldo);

        return account;
    }

    private async Task SetCacheAsync(int id, int limite, int saldo)
    {
        var limiteBytes = BitConverter.GetBytes(limite);
        var saldoBytes = BitConverter.GetBytes(saldo);

        await Task.WhenAll(
            cache.SetAsync($"{AccountHashPrefix}{id}:{Limite}", limiteBytes),
            cache.SetAsync($"{AccountHashPrefix}{id}:{Saldo}", saldoBytes));
    }

    static readonly DistributedCacheEntryOptions cacheOptions = new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(50) };
    public async Task<BankStatement> GetBankStatementAsync(int id)
    {
        string key = $"bankStatement:{id}";
        var statement = await cache.GetStringAsync(key);

        BankStatement bankStatement = null;
        if (string.IsNullOrEmpty(statement))
        {
            bankStatement = await next.GetBankStatementAsync(id);
            var stringified = JsonSerializer.Serialize(bankStatement, options);
            await cache.SetStringAsync(key, stringified, cacheOptions);
        }

        return bankStatement;
    }

    public async Task Save(Transaction transaction)
    {
        await SetCacheAsync(transaction.AccountId, transaction.Limite, transaction.Saldo);
    }
}
