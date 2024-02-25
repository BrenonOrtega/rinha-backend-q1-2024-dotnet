using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using NATS.Client.KeyValueStore;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;

public sealed class NatsDecoratedRepo : IDecoratedRepository
{
    private readonly INatsKVStore cache;
    private readonly IRepository next;

    public NatsDecoratedRepo(INatsKVStore cache, IRepository next)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task<Account> GetAccountByIdAsync(int id)
    {
        try
        {
            var entry = await cache.GetEntryAsync<Account>(id.ToString());
            return entry.Value;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return await next.GetAccountByIdAsync(id);
    }

    public async Task Save(Transaction transaction)
    {
        await cache.UpdateAsync(
            key: transaction.AccountId.ToString(),
            value: new Account(transaction.AccountId, transaction.Limite, transaction.Valor),
            revision: (ulong)transaction.RealizadaEm.Ticks);
    }

    public Task<BankStatement> GetBankStatementAsync(int id)
    {
        return next.GetBankStatementAsync(id);
    }
}
