using System.Text.Json.Serialization;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public readonly struct Balance(long total, long limite)
{
    public long Total { get; } = total;

    [JsonPropertyName("data_extrato")]
    public DateTime DataExtrato { get; } = DateTime.Now;

    public long Limite { get; } = limite;
}