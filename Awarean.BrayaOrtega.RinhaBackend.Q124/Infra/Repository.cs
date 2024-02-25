using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;

public class Repository : IRepository
{
    private readonly RinhaBackendDbContext context;

    public Repository(RinhaBackendDbContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Account> GetAccountByIdAsync(int id)
    {
        var transaction = await context.Set<Transaction>()
            .AsNoTracking()
            .Where(x => x.AccountId == id)
            .OrderByDescending(x => x.RealizadaEm)
            .Select(x => new { x.AccountId, x.Saldo, x.Limite })
            .FirstOrDefaultAsync();

        if (transaction is null)
            return null;

        return new Account(transaction.AccountId, transaction.Limite, transaction.Saldo);
    }

    public async Task<BankStatement> GetBankStatementAsync(int id)
    {
        var queriedTransactions = await context.Set<Transaction>()
            .AsNoTracking()
            .Where(x => x.AccountId == id)
            .OrderByDescending(x => x.RealizadaEm)
            .Take(10)
            .ToListAsync();
        
        var first = queriedTransactions.FirstOrDefault();
        
        if (first is null)
            return null;

        var balance = new Balance(first.Saldo, first.Limite);

        var transactions = queriedTransactions
            .Where(x => x.Descricao is not null)
            .Select(x => new BankStatementTransaction(x.Valor, x.Tipo, x.Descricao, x.RealizadaEm))
            .ToList();

        return new BankStatement(balance, transactions);
    }

    public Task Save(Transaction transaction)
    {
        return Task.CompletedTask;
    }
}
