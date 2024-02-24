
using System.Text.Json.Serialization;
using MemoryPack;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

[MemoryPackable]
public sealed partial class Account
{
    [JsonConstructor]
    [MemoryPackConstructor]
    public Account(int id, int limite, int saldo)
    {
        Id = id;
        Limite = limite;
        Saldo = saldo;
    }

    public Account() : this(default, default, default)
    {
    }

    public int Id { get; private set; }

    public int Limite { get; private set;}

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
