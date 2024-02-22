namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public sealed class BankStatementTransaction(int valor, char tipo, string descricao, DateTime realizadaEm)
{
    public int Valor { get; } = valor;

    public char Tipo { get; } = tipo;
    
    public string Descricao { get; } = descricao;
    
    public DateTime RealizadaEm { get; } = realizadaEm;

    internal bool IsEmpty() => Descricao is null ||
            Valor == default
            && Tipo == default
            && Descricao == default
            && RealizadaEm == default;
}
