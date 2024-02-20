namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public sealed class Account
{
    public Account(long limite, long saldo)
        : this(default, limite, saldo)
    {

    }

    public Account(int id, long limite, long saldo)
    {
        Id = id;
        Limite = limite;
        Saldo = saldo;
    }

    public int Id { get; set; }

    public long Limite { get; set; }

    public long Saldo { get; set; }

    public bool CanExecute(TransactionRequest transaction)
    {
        if (transaction.Tipo == "d")
        {
            long remainingLimit = GetRemainingLimit();
            return transaction.Valor <= remainingLimit;
        }

        return true;
    }

    private long GetRemainingLimit() => Limite + Saldo;

    public Transaction Execute(TransactionRequest transaction)
    {
        long valor = transaction.Valor;

        _ = transaction.Tipo switch
        {
            "c" => Saldo += valor,
            "d" => Saldo -= valor,
            _ => throw new InvalidOperationException("Operacao nao permitida. As operacoes permitidas sao (c) Credito e (d) Debito.")
        };

        if (GetRemainingLimit() < 0)
            throw new InvalidOperationException();

        return new Transaction(valor, transaction.Tipo, transaction.Descricao, Id);
    }
}
