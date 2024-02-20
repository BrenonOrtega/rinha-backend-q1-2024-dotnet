namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public sealed class Account
{
    private const char Credit = 'c';
    private const char Debit = 'd';

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
        var transactionType = transaction.Tipo;

        if (transactionType is not Credit and not Debit)
            return false;

        if (transaction.Descricao is { Length: 0 or > 10 })
            return false; 


        if (transactionType is Debit)
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

        if (transaction.Tipo is Credit)
             Saldo += valor;
        else 
            Saldo -= valor;

        return new Transaction(valor, transaction.Tipo, transaction.Descricao, Id);
    }
}
