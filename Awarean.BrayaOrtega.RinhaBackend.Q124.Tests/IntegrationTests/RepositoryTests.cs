using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Microsoft.Extensions.DependencyInjection;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using FluentAssertions;
using Npgsql;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Tests.IntegrationTests;

public sealed class RepositoryTests : IDisposable
{
    private readonly IRepository repo;

    [Fact]
    public async Task Saving_Account_Should_Succeed()
    {
        var transaction = new Transaction(5, 'c', "existing", 1, 50_000, 0, DateTime.UtcNow);

        await repo.Save(transaction);

        var actual = await repo.GetAccountByIdAsync(transaction.AccountId);

        actual.Limite.Should().Be(transaction.Limite);
        actual.Saldo.Should().Be(transaction.Saldo);
        actual.Id.Should().Be(transaction.AccountId);
    }

    [Fact]
    public async Task Getting_Bank_Statement_Should_Work()
    {
        var transaction = new Transaction(5, 'c', "existing", 1, 50_000, 0, DateTime.UtcNow);

        await repo.Save(transaction);

        var statement = await repo.GetBankStatementAsync(transaction.AccountId);

        statement.Saldo.Total.Should().Be(transaction.Saldo);
        statement.Saldo.Limite.Should().Be(transaction.Limite);

        statement.UltimasTransacoes.Should()
            .ContainEquivalentOf(
                new BankStatementTransaction(transaction.Valor, transaction.Tipo, transaction.Descricao, transaction.RealizadaEm));
    }

    public RepositoryTests()
    {
        const string connectionString = "Server=localhost;Port=5432;Database=rinha_database;User Id=postgres;Password=postgres;Include Error Detail=true;";
        const string redisString = "localhost:6379,password=redis,ssl=False";

        var services = new ServiceCollection()
            .ConfigureInfrastructure(connectionString, redisString)
            .BuildServiceProvider();

        repo = services.GetRequiredService<IRepository>();
    }

    public void Dispose()
    {
    }
}
