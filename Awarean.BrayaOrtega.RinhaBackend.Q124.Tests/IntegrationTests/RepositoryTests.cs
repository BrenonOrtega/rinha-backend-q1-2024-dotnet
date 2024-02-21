using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Microsoft.Extensions.DependencyInjection;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using FluentAssertions;
using Npgsql;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Tests.IntegrationTests;

public sealed class RepositoryTests : IDisposable
{
    private readonly NpgsqlDataSource dataSource;
    private readonly IRepository repo;

    [Fact]
    public async Task Saving_Account_Should_Succeed()
    {
        var account = new Account(10_000_000, 50_000);
        var transaction = new Transaction(1, 'c', "aaaa", 1);

        await repo.Save(account, transaction);

        var existing = await repo.GetAccountByIdAsync(account.Id);

        existing.Should().BeEquivalentTo(account);
    }

    [Fact]
    public async Task Getting_Bank_Statement_Should_Work()
    {
        var transaction = new Transaction(5, 'c', "existing", 1);
        var account = new Account(1, 10_000_000, 50_000);

        await repo.Save(account, transaction);

        var statement = await repo.GetBankStatementAsync(account.Id);

        statement.Saldo.Total.Should().Be(account.Saldo);
        statement.Saldo.Limite.Should().Be(account.Limite);

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

        dataSource = services.GetRequiredService<NpgsqlDataSource>();
        repo = services.GetRequiredService<IRepository>();
    }

    public void Dispose()
    {
        using var conn = dataSource.CreateConnection();
        using var command = new NpgsqlCommand(@"
            BEGIN;
            TRUNCATE TABLE Transactions;
            COMMIT;", conn);

        conn.Open();

        command.ExecuteNonQuery();
    }
}
