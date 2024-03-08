namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public readonly partial struct ExecuteTransactionResponse(bool isSuccess, int limite, int saldo, TransactionExecutionError? error = null)
{
    public int Limite { get; } = limite;
    public int Saldo { get; } = saldo;
    public bool IsSuccess { get; } = isSuccess;
    public TransactionExecutionError? Error { get; } = error;
}
