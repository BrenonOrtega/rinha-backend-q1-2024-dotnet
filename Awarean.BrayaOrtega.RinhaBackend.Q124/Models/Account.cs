
namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public struct Account
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

    public Account() : this(default, default, default)
    {
    }

    public int Id { get; }

    public long Limite { get; }

    public long Saldo { get; private set;}

    public readonly bool CanExecute(TransactionRequest transaction)
    {
        var transactionType = transaction.Tipo;

        if (transactionType is not Transaction.Credit and not Transaction.Debit)
            return false;

        if (transaction.Descricao is null or { Length: 0 or > 10 })
            return false; 

        if (transactionType is Transaction.Debit)
        {
            long remainingLimit = GetRemainingLimit();
            return transaction.Valor <= remainingLimit;
        }

        return true;
    }

    private readonly long GetRemainingLimit() => Limite + Saldo;

    public Transaction Execute(TransactionRequest transaction)
    {
        long valor = transaction.Valor;

        if (transaction.Tipo is Transaction.Credit)
             Saldo += valor;
        else 
            Saldo -= valor;

        return new Transaction(valor, transaction.Tipo, transaction.Descricao, Id);
    }

    internal readonly bool IsEmpty() => Id is 0 && Saldo is 0 && Limite is 0;
}
