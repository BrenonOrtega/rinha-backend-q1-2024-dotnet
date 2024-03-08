using System.Text.Json.Serialization;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public sealed partial class TransactionRequest
{
    public TransactionRequest(int valor, char tipo, string descricao)
    {
        Valor = valor;
        Tipo = tipo;
        Descricao = descricao;
    }

    [JsonPropertyName("valor")]
    public int Valor { get; }

    [JsonPropertyName("tipo")]
    public char Tipo { get; }

    [JsonPropertyName("descricao")]
    public string Descricao { get; }

    public bool IsInvalid()
    {
        if (Tipo is not Transaction.Credit and not Transaction.Debt)
            return false;

        if (Descricao is null or { Length: 0 or > 10 })
            return false;

        if (Valor < 0)
            return false;

        return true;
    } 
}
