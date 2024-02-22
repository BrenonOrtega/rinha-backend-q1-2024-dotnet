using System.Text.Json.Serialization;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public sealed class TransactionRequest(int valor, char tipo, string descricao)
{
    [JsonPropertyName("valor")]
    public int Valor { get; } = valor;

    [JsonPropertyName("tipo")]
    public char Tipo { get; } = tipo;

    [JsonPropertyName("descricao")]
    public string Descricao { get; } = descricao;

    public bool IsInvalid() 
        => Valor < 0 
        || Tipo is not Transaction.Credit and not Transaction.Debit
        || string.IsNullOrEmpty(Descricao) 
        || Descricao.Length > 10;
}
