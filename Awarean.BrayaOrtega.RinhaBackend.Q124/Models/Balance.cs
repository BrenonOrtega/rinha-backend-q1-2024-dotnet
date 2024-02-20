using System.Text.Json.Serialization;
using Dapper;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

[type: DapperAot]
public struct Balance(long total, long limite)
{
    public Balance() : this(0, 0)
    {

    }

    public long Total { get; set; } = total;

    [JsonPropertyName("data_extrato")]
    public DateTime DataExtrato { get; } = DateTime.Now;

    public long Limite { get; set; } = limite;
}