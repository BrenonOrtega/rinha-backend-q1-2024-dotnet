using Awarean.BrayaOrtega.RinhaBackend.Q124;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateSlimBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddLogging();
builder.Services.AddHttpLogging(x => { x.LoggingFields = HttpLoggingFields.All; });
builder.Services.ConfigureInfrastructure(builder.Configuration.GetConnectionString("Postgres"));

var app = builder.Build();

app.MapGet("/", () => "Im working!")
    .WithHttpLogging(HttpLoggingFields.All);

app.MapGet("/clientes/{id:int}/extrato", 
    (int id, [FromServices]Repository repo) =>
    {
        var account = repo.GetBankStatementAsync(id);

        if (account is not null)
            return Results.Ok(account);

        return Results.NotFound();
    });

app.MapPost("/clientes/{id:int}/transacoes", async (
    int id, 
    [FromBody]TransactionRequest transaction,
    [FromServices] Repository repo) =>
    {
        var account = await repo.GetAccountByIdAsync(id);

        if (account is null)
            return Results.NotFound();

        if (account.CanExecute(transaction) is false)
            return Results.UnprocessableEntity();

        account.Execute(transaction);
        
        await repo.SaveChanges();

        return Results.Ok();
    })
    .WithHttpLogging(HttpLoggingFields.All);

app.Run();
