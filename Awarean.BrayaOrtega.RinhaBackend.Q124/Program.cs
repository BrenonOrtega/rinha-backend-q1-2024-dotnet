using System.Text.Json.Serialization;
using Awarean.BrayaOrtega.RinhaBackend.Q124;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddLogging();
builder.Services.ConfigureInfrastructure(builder.Configuration.GetConnectionString("Postgres"));

var app = builder.Build();

app.MapGet("/clientes/{id:int}/extrato",
    async (int id, [FromServices] Repository repo) =>
    {
        var bankStatement = await repo.GetBankStatementAsync(id);

        if (bankStatement is not null)
            return Results.Ok(bankStatement.Value);

        return Results.NotFound();
    });

app.MapPost("/clientes/{id:int}/transacoes", async (
    int id,
    [FromBody] TransactionRequest transaction,
    [FromServices] Repository repo) =>
    {
        var account = await repo.GetAccountByIdAsync(id);

        if (account is null)
            return Results.NotFound();

        if (account.CanExecute(transaction) is false)
            return Results.UnprocessableEntity();

        var createdTransaction = account.Execute(transaction);

        await repo.Save(account, createdTransaction);

        return Results.Ok();
    });

app.Run();

[JsonSerializable(typeof(TransactionRequest))]
[JsonSerializable(typeof(TransactionResponse))]
[JsonSerializable(typeof(BankStatement))]
[JsonSerializable(typeof(Balance))]
[JsonSerializable(typeof(BankStatementTransaction))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}