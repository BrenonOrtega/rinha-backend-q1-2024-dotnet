namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public readonly struct TransactionResponse(long limite, long saldo)
{
    public long Limite { get; } = limite;
    public long Saldo { get; } = saldo;
}
