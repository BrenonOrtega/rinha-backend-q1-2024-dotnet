using System.Text.Json.Serialization;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public sealed class Balance(int total, int limite)
{
    public Balance() : this(0, 0)
    {

    }

    public int Total { get; } = total;

    [JsonPropertyName("data_extrato")]
    public DateTime DataExtrato { get; } = DateTime.Now;

    public int Limite { get; } = limite;

    internal bool IsEmpty() => Total is 0 && Limite is 0;
}