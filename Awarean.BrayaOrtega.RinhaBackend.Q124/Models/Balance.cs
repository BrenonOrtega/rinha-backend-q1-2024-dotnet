using System.Text.Json.Serialization;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

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