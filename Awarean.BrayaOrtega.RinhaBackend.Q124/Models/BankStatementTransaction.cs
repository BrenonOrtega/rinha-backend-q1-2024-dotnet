using System.Text.Json.Serialization;
using Dapper;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

[type: DapperAot]
public struct BankStatementTransaction(long valor, string tipo, string descricao, DateTime realizadaEm)
{
    public long Valor { get; } = valor;

    public string Tipo { get; } = tipo;
    
    public string Descricao { get; } = descricao;
    
    public DateTime RealizadaEm { get; } = realizadaEm;
}
