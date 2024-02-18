using System.Text.Json.Serialization;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public readonly struct Balance(int total, int limite)
{
    public int Total { get; } = total;

    [JsonPropertyName("data_extrato")]
    public DateTime DataExtrato { get; } = DateTime.Now;

    public int Limite { get; } = limite;
}