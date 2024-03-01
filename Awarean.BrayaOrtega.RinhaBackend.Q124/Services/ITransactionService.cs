using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Services;

public interface ITransactionService 
{
    Task<Account> TryExecuteTransactionAsync(int accountId, TransactionRequest request);

    Task<ExecuteTransactionResponse> GetBankstatementAsync(int id);
}