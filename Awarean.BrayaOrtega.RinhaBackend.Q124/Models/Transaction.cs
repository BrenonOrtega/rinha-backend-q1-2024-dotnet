namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public sealed class Transaction(long valor, char tipo, string descricao, int accountId)
{
    public long Valor { get; } = valor;

    public char Tipo { get; } = tipo;
    
    public string Descricao { get; } = descricao;
    
    public int AccountId { get; } = accountId;
    
    public DateTime RealizadaEm { get; }
}
