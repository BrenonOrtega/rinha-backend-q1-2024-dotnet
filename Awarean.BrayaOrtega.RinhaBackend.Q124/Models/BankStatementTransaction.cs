namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public struct BankStatementTransaction(long valor, char tipo, string descricao, DateTime realizadaEm)
{
    public long Valor { get; } = valor;

    public char Tipo { get; } = tipo;
    
    public string Descricao { get; } = descricao;
    
    public DateTime RealizadaEm { get; } = realizadaEm;
}
