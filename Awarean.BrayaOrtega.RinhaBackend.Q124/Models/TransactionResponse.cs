using MemoryPack;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

[MemoryPackable]
public readonly partial struct TransactionResponse(long limite, long saldo)
{
    public long Limite { get; } = limite;
    public long Saldo { get; } = saldo;
}
