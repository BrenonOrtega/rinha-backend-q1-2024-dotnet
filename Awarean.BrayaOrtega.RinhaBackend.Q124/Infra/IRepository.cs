using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;

public interface IRepository
{
    Task<BankStatement> GetBankStatementAsync(int id);
    Task Save(Account account, Transaction transaction);
    Task<Account> GetAccountByIdAsync(int id);

}

public interface IDecoratedRepository : IRepository
{
    
}