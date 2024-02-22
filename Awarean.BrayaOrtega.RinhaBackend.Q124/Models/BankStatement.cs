using System.Text.Json.Serialization;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public sealed class BankStatement
{
    public BankStatement(Balance saldo, List<BankStatementTransaction> ultimasTransacoes)
    {
        Saldo = saldo;
        UltimasTransacoes = ultimasTransacoes;
    }

    public BankStatement()
    {
        Saldo = new Balance();
        UltimasTransacoes = [];
    }

    public Balance Saldo { get; }

    [JsonPropertyName("ultimas_transacoes")]
    public IList<BankStatementTransaction> UltimasTransacoes { get; } = [];

    public bool IsEmpty() => Saldo.IsEmpty();
}
