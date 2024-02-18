using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using Microsoft.EntityFrameworkCore;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;

public class Repository
{
    private readonly DbContext context;

    public Repository(DbContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }

    internal Task SaveChanges()
    {
        return context.SaveChangesAsync();
    }

    public async Task<Account> GetAccountByIdAsync(int id)
    {
        var account = await context.Set<Account>().FirstOrDefaultAsync(x => x.Id == id);

        return account;
    }

    public Task<BankStatement> GetBankStatementAsync(int id)
    {
        throw new NotImplementedException();
    }
}
