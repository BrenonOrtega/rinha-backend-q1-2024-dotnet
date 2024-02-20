using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public sealed class TransactionRequest(long valor, char tipo, string descricao)
{
    [Required]
    [Range(0, long.MaxValue)]
    [JsonPropertyName("valor")]
    public long Valor { get; } = valor;

    [Required]
    [AllowedValues("c", "d", "C", "D")]
    [JsonPropertyName("tipo")]
    public char Tipo { get; } = tipo;

    [Required]
    [JsonPropertyName("descricao")]
    public string Descricao { get; } = descricao;
}
