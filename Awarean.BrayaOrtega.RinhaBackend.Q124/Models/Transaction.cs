using System.Text.Json.Serialization;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public class Transaction
{
    public const char Credit = 'c';
    public const char Debt = 'd';

    [JsonConstructor]
    public Transaction(int valor, char tipo, string descricao, int accountId,
        int limite, int saldo, DateTime realizadaEm)
    {
        Valor = valor;
        Tipo = tipo;
        Descricao = descricao;
        AccountId = accountId;
        Limite = limite;
        Saldo = saldo;
        RealizadaEm = realizadaEm;
    }

    public int Valor { get; }

    public char Tipo { get; }

    public string Descricao { get; }

    public int AccountId { get; }

    public int Limite { get; }

    public int Saldo { get; private set; }

    public DateTime RealizadaEm { get; private set; }

    internal void UpdateSaldo(long newSaldo)
    {
        Saldo = (int)newSaldo;
        RealizadaEm = DateTime.Now;
    }
}
