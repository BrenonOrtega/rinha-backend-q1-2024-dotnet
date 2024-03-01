namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public readonly partial struct ExecuteTransactionResponse(bool isSuccess, long limite, long saldo, TransactionExecutionError? error = null)
{
    public long Limite { get; } = limite;
    public long Saldo { get; } = saldo;
    public bool IsSuccess { get; } = isSuccess;
    public TransactionExecutionError? Error { get; } = error;
}
