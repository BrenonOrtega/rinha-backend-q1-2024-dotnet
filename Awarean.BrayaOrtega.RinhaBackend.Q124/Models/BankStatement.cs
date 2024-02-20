using System.Text.Json.Serialization;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public struct BankStatement
{
    public BankStatement(Balance saldo, List<BankStatementTransaction> ultimasTransacoes)
    {
        Saldo = saldo;
        UltimasTransacoes = ultimasTransacoes;
    }

    public BankStatement()
    {

    }

    public Balance Saldo { get; set; }

    [JsonPropertyName("ultimas_transacoes")]
    public List<BankStatementTransaction> UltimasTransacoes { get; } = [];
}
