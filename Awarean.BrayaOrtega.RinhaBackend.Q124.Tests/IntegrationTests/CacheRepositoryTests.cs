
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using FluentAssertions;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Tests.IntegrationTests;

public class CacheRepositoryTests : IDisposable
{
    private readonly ConnectionMultiplexer cache;
    private readonly IRepository next;
    private readonly IRepository repo;

    public CacheRepositoryTests()
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
            .AddLogging()
            .ConfigureInfrastructure(config)
            .BuildServiceProvider();

        cache = services.GetRequiredService<ConnectionMultiplexer>();
        next = Substitute.For<IRepository>();
        var logger = services.GetRequiredService<ILogger<CacheRepository>>();
        repo = new CacheRepository(cache, next, logger);
    }

    [Fact]
    public async Task Should_Save_Correctly()
    { 
        var transaction = new Transaction(
            valor: 20,
            tipo: 'c',
            descricao: "a",
            accountId: 1,
            limite: 20000,
            saldo: 0,
            realizadaEm: DateTime.UtcNow);

        await repo.Save(transaction);

        var actual = await repo.GetAccountByIdAsync(transaction.AccountId);

        actual.Limite.Should().Be(transaction.Limite);
        actual.Saldo.Should().Be(transaction.Saldo);
        actual.Id.Should().Be(transaction.AccountId);
    }

    [Fact]
    public async Task Should_Get_From_Next_When_Not_Found()
    {
        var account = new Account(9999, 20000, 0);
        next.GetAccountByIdAsync(Arg.Is(account.Id)).Returns(account);

        var actual = await repo.GetAccountByIdAsync(account.Id);

        actual.Should().BeEquivalentTo(account);
        await next.Received(1).GetAccountByIdAsync(Arg.Is(account.Id));
    }

    [Fact]
    public async Task SavingCreditShouldDoAutomatically()
    {
        // Given
        var transaction = new Transaction(250, Transaction.Credit, "deposito", 4, 0, 0, DateTime.Now);
        // When

        var result = await repo.Save(transaction);
    
        // Then
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SavingCreditShouldIncrementAccountSnapshot()
    {
        // Given
        var transaction = new Transaction(250, Transaction.Credit, "deposito", 4, 0, 0, DateTime.Now);

        // When
        await repo.Save(transaction);

        var actual = await repo.GetAccountByIdAsync(transaction.AccountId);
    
        // Then
        actual.Saldo.Should().Be(transaction.Valor);
    }

    [Fact]
    public async Task SavingDebtShouldDecrementAccountSnapshot()
    {
        var existingTransaction = new Transaction(250, Transaction.Credit, "deposito", 4, 0, 0, DateTime.Now);
        await repo.Save(existingTransaction);

        var debtTransaction = new Transaction(existingTransaction.Valor,
            Transaction.Debt, "debito", existingTransaction.AccountId, 0, 0, DateTime.Now);

        await repo.Save(debtTransaction);
    
        var expectedSaldo = debtTransaction.Valor - existingTransaction.Valor;
        var actual = await repo.GetAccountByIdAsync(debtTransaction.AccountId);
        actual.Saldo.Should().Be(expectedSaldo);
    }

    [Fact]
    public async Task Invalid_Debt_Transaction_Should_Fail()
    {
        var accountId = 9;
        var existingTransaction = new Transaction(
            valor: 250,
            tipo: Transaction.Credit,
            descricao: "deposito",
            accountId: accountId,
            limite: 0,
            saldo: 0,
            realizadaEm: DateTime.Now);
            
        await repo.Save(existingTransaction);

        var invalidDebtTransaction = new Transaction(
            valor: existingTransaction.Valor + 1,
            tipo: Transaction.Debt,
            descricao: "debito",
            accountId: accountId,
            limite: 0,
            saldo: 0,
            realizadaEm: DateTime.Now);

        var result = await repo.Save(invalidDebtTransaction);
    
        result.Should().BeFalse();
    }

    [Fact]
    public async Task And_Account_Is_Not_Found_Should_Fail()
    {
        var inexistentId = 20;
        var invalidDebtTransaction = new Transaction(
            valor: 1,
            tipo: Transaction.Debt,
            descricao: "debito",
            accountId: inexistentId,
            limite: 0,
            saldo: 0,
            realizadaEm: DateTime.Now);

        var result = await repo.Save(invalidDebtTransaction);
    
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Transaction_Should_Be_Updated_If_Occurs_With_Differente_Balance_Values()
    {
        var account = new Account(9, limite: 0, saldo: 0);

        //Debt created
        TransactionRequest debtRequest = new TransactionRequest(
                    valor: account.Saldo + 1,
                    tipo: Transaction.Debt,
                    descricao: "alterValue");

        var debt = account.Execute(debtRequest);
        
        // Credit occurs before debt (concurrency)
        var credit = account.Execute(new TransactionRequest(
            valor: 250,
            tipo: Transaction.Credit,
            descricao: "deposito"));

        await repo.Save(credit);

        //Debt is finally saved.
        await repo.Save(debt);

        var actual = await repo.GetBankStatementAsync(account.Id);
    
        var actualDebt = actual.UltimasTransacoes.First();
        var expectedSaldo = credit.Valor - debtRequest.Valor;
        
        //This ensures the maths match.
        actual.Saldo.Total.Should().Be(expectedSaldo);
        
        // This ensures database will be consistent when saved.
        actual.Saldo.Total.Should().Be(debt.Saldo);

        actualDebt.RealizadaEm.Should().BeCloseTo(debt.RealizadaEm, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Refused_Transaction_Should_Not_Send_In_Statement_List()
    {
        var creditTransaction =  new Transaction(
            valor: 1,
            tipo: Transaction.Credit,
            descricao: "credito",
            accountId: 10,
            limite: 0,
            saldo: 0,
            realizadaEm: DateTime.Now.AddHours(-1));

        await repo.Save(creditTransaction);

        var invalidDebtTransaction = new Transaction(
            valor: creditTransaction.Valor + 1,
            tipo: Transaction.Debt,
            descricao: "debito",
            accountId: creditTransaction.AccountId,
            limite: 0,
            saldo: 0,
            realizadaEm: DateTime.Now);

        await repo.Save(invalidDebtTransaction);
    
        var bankStatement = await repo.GetBankStatementAsync(creditTransaction.AccountId);

        var actualLastTransaction = bankStatement.UltimasTransacoes.First();
        var equivalent = new BankStatementTransaction(
            invalidDebtTransaction.Valor,
            invalidDebtTransaction.Tipo,
            invalidDebtTransaction.Descricao,
            invalidDebtTransaction.RealizadaEm
        );

        actualLastTransaction.Should().NotBeEquivalentTo(equivalent);
    }

    [Fact]
    public async Task When_Queried_Account_Value_Is_Equal_To_Debt_Transaction_Shouldnt_Update_Transaction()
    {
        
    }

    public void Dispose()
    {
        _ = cache.GetDatabase().ExecuteAsync("FLUSHDB").GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
