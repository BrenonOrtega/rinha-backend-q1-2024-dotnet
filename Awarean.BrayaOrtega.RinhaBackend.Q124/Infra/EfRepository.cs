// using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
// using Microsoft.EntityFrameworkCore;

// namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;

// public class EfRepository
// {
//     private readonly DbContext context;

//     public EfRepository(RinhaBackendDbContext context)
//     {
//         this.context = context ?? throw new ArgumentNullException(nameof(context));
//     }

//     internal Task SaveChanges()
//     {
//         return context.SaveChangesAsync();
//     }

//     public async Task<Account> GetAccountByIdAsync(int id)
//     {
//         var account = await context.Set<Account>().FirstOrDefaultAsync(x => x.Id == id);

//         return account;
//     }

//     public async Task<BankStatement?> GetBankStatementAsync(int id)
//     {
//         var projection = await context.Set<Account>()
//             .Where(x => x.Id == id)
//             .Select(x => new { x.Limite, x.Saldo, Transactions = x.Transactions.Select(t => new { t.Valor, t.RealizadaEm, t.Descricao, t.Tipo }) })
//             .FirstOrDefaultAsync();

//         if (projection is null)
//             return null;

//         return new BankStatement(
//             new Balance(projection.Saldo, projection.Limite), 
//             projection.Transactions.Select(x => new BankStatementTransaction(x.Valor, x.Tipo, x.Descricao, x.RealizadaEm)).ToList());
//     }

//     public async Task SaveAsync(Account account)
//     {
//         context.Set<Account>().Add(account);

//         await context.SaveChangesAsync();
//     }
// }
