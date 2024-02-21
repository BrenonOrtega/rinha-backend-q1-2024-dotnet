using System.Text.Json.Serialization;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

[JsonSerializable(typeof(TransactionRequest))]
[JsonSerializable(typeof(TransactionResponse))]
[JsonSerializable(typeof(BankStatement))]
[JsonSerializable(typeof(Balance))]
[JsonSerializable(typeof(BankStatementTransaction))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}