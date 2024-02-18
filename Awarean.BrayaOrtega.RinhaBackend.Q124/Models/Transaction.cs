namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public sealed class Transaction(long valor, string tipo, string descricao, DateTime? realizadaEm = null)
{
    public long Valor { get; } = valor;
    public string Tipo { get; } = tipo;
    public string Descricao { get; } = descricao;
    public DateTime RealizadaEm { get; } = realizadaEm ?? DateTime.Now;
}
