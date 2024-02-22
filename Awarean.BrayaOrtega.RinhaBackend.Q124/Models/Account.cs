
namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public sealed class Account
{
    public Account(int limite, int saldo)
        : this(default, limite, saldo)
    {

    }

    public Account(int id, int limite, int saldo)
    {
        Id = id;
        Limite = limite;
        Saldo = saldo;
    }

    public Account() : this(default, default, default)
    {
    }

    public int Id { get; }

    public int Limite { get; }

    public int Saldo { get; private set;}

    public bool CanExecute(TransactionRequest transaction)
    {
        var transactionType = transaction.Tipo;

        if (transactionType is not Transaction.Credit and not Transaction.Debit)
            return false;

        if (transaction.Descricao is null or { Length: 0 or > 10 })
            return false; 

        if (transactionType is Transaction.Debit)
        {
            int remainingLimit = GetRemainingLimit();
            return transaction.Valor <= remainingLimit;
        }

        return true;
    }

    private int GetRemainingLimit() => Limite + Saldo;

    public Transaction Execute(TransactionRequest transaction)
    {
        int valor = transaction.Valor;

        if (transaction.Tipo is Transaction.Credit)
             Saldo += valor;
        else 
            Saldo -= valor;

        return new Transaction(valor, transaction.Tipo, transaction.Descricao, Id, Limite, Saldo, DateTime.Now);
    }

    internal bool IsEmpty() => Id is 0 && Saldo is 0 && Limite is 0;
}
