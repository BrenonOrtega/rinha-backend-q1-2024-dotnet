using System.Text.Json.Serialization;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public sealed partial class Balance
{
    public Balance(int total, int limite)
    {
        Total = total;
        Limite = limite;
    }

    public Balance() : this(0, 0)
    {

    }

    public int Total { get; }

    [JsonPropertyName("data_extrato")]
    public DateTime DataExtrato { get; } = DateTime.Now;

    public int Limite { get; }

    internal bool IsEmpty() => Total is 0 && Limite is 0;
}