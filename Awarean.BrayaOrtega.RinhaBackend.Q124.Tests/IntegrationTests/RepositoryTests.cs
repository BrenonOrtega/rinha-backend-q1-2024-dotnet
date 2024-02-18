using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Microsoft.Extensions.DependencyInjection;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Tests.IntegrationTests;

public sealed class RepositoryTests : IDisposable
{
    private readonly RinhaBackendDbContext context;
    private readonly IDbContextTransaction transaction;
    private readonly Repository repo;

    [Fact]
    public async Task Saving_Account_Should_Succeed()
    {
        var account = new Account(10_000_000, 50_000);

        await repo.SaveAsync(account);

        var existing = await repo.GetAccountByIdAsync(account.Id);

        existing.Should().BeEquivalentTo(account);
    }

    [Fact]
    public async Task Getting_Bank_Statement_Should_Work()
    {
        var account = new Account(1, 10_000_000, 50_000, [ new Transaction(5, "c", "existing", 1)]);

        await repo.SaveAsync(account);

        var statement = await repo.GetBankStatementAsync(account.Id);

        statement.Value.Saldo.Total.Should().Be(account.Saldo);
        statement.Value.Saldo.Limite.Should().Be(account.Limite);

        statement.Value.UltimasTransacoes.Should()
            .ContainEquivalentOf(
                account.Transactions
                    .Select(x=> new BankStatementTransaction(x.Valor, x.Tipo, x.Descricao, x.RealizadaEm))
                    .Single());
    }
 
    public RepositoryTests()
    {
        const string connectionString = "Server=localhost;Port=5432;Database=rinha_backend;User Id=postgres;Password=postgres;Include Error Detail=true;";
        
        var services = new ServiceCollection()
            .ConfigureInfrastructure(connectionString)
            .BuildServiceProvider();
        
        context = services.GetRequiredService<RinhaBackendDbContext>();
        context.Database.EnsureCreated();
        transaction = context.Database.BeginTransaction();
        repo = services.GetRequiredService<Repository>();
    }

    public void Dispose()
    {
        transaction.Rollback();
        context.Database.EnsureDeleted();
        context.Dispose();
    }
}
