using System.Text.Json.Serialization;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public record BankStatement
{
    public Balance Saldo { get; private set;}

    [JsonPropertyName("ultimas_transacoes")]
    public List<Transaction> UltimasTransacoes { get; private set; } = [];
}
