using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Services;

public interface ITransactionService 
{
    Task<ExecuteTransactionResponse> TryExecuteTransactionAsync(int accountId, TransactionRequest request, CancellationToken token);

    Task<BankStatement> GetBankStatementAsync(int id);
}