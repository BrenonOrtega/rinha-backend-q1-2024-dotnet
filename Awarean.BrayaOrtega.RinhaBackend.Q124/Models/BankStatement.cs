using System.Text.Json.Serialization;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public readonly struct BankStatement
{
    public BankStatement(Balance saldo, List<BankStatementTransaction> ultimasTransacoes)
    {
        Saldo = saldo;
        UltimasTransacoes = ultimasTransacoes;
    }

    public Balance Saldo { get; }

    [JsonPropertyName("ultimas_transacoes")]
    public List<BankStatementTransaction> UltimasTransacoes { get; } = [];
}

public struct BankStatementTransaction(long valor, string tipo, string descricao, DateTime realizadaEm)
{
    public long Valor { get; } = valor;

    public string Tipo { get; } = tipo;
    
    public string Descricao { get; } = descricao;
    
    public DateTime RealizadaEm { get; } = realizadaEm;
}
