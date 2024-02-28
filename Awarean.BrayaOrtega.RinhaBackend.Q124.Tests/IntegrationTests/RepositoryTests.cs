using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Microsoft.Extensions.DependencyInjection;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using FluentAssertions;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Tests.IntegrationTests;

public sealed class RepositoryTests : IDisposable
{
    private readonly IRepository repo;
    private readonly NpgsqlDataSource pg;

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
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([
                new ("ConnectionStrings:Postgres", connectionString),
                new("ConnectionStrings:Redis", redisString),
            ])
            .Build();
        var services = new ServiceCollection()
            .ConfigureInfrastructure(config)
            .BuildServiceProvider();

        repo = services.GetRequiredService<IRepository>();
        pg = services.GetRequiredService<NpgsqlDataSource>();
    }

    public void Dispose()
    {
        using var conn = pg.OpenConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "DELETE * FROM transactions WHERE Id > 5;";
        cmd.ExecuteNonQuery();
        conn.Close();
    }
}
