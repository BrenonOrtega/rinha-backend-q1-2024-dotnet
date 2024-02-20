using Dapper;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

[type:DapperAot]
public sealed class Transaction(long valor, string tipo, string descricao, int accountId)
{
    public long Valor { get; } = valor;

    public string Tipo { get; } = tipo;
    
    public string Descricao { get; } = descricao;
    
    public int AccountId { get; } = accountId;
    
    public DateTime RealizadaEm { get; }
}
