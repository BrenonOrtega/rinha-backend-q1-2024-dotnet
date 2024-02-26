using System.Text.Json.Serialization;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public sealed partial class BankStatementTransaction
{
    public BankStatementTransaction(int valor, char tipo, string descricao, DateTime realizadaEm)
    {
        Valor = valor;
        Tipo = tipo;
        Descricao = descricao;
        RealizadaEm = realizadaEm;
    }
    public int Valor { get; }

    public char Tipo { get; }
    
    public string Descricao { get; }
    
    [JsonPropertyName("realizada_em")]
    public DateTime RealizadaEm { get; }

    internal bool IsEmpty() => Descricao is null ||
            Valor == default
            && Tipo == default
            && Descricao == default
            && RealizadaEm == default;
}
