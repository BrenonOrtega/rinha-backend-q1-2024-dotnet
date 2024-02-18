using System.ComponentModel.DataAnnotations;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public readonly struct TransactionRequest(long valor, string tipo, string descricao)
{
    [Required]
    [Range(0, long.MaxValue)]
    public long Valor { get; } = valor;
    [Required]
    [AllowedValues("c", "d", "C", "D")]
    public string Tipo { get; } = tipo;
    [Required]
    [Length(0, 10)]
    public string Descricao { get; } = descricao;
}
