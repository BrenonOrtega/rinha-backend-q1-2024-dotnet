
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
using Awarean.BrayaOrtega.RinhaBackend.Q124.Services;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Tests.IntegrationTests;

public class CacheRepositoryTests : IDisposable
{
    private readonly ConnectionMultiplexer cache;
    private readonly IRepository next;
    private readonly ITransactionService service;

    public CacheRepositoryTests()
    {
        const string connectionString = "Server=localhost;Port=5432;Database=rinha_database;User Id=postgres;Password=postgres;Include Error Detail=true;";
        const string redisString = "localhost:6379,password=redis,ssl=False";
        const string natsString = "localhost:4222";
        const string natsOwn = "Transaction";
        const string natsDestination = "Transaction";

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([
                new("ConnectionStrings:Postgres", connectionString),
                new("ConnectionStrings:Redis", redisString),
                new("ConnectionStrings:Nats", natsString),
                new("NATS_OWN", natsOwn),
                new("NATS_DESTINATION", natsDestination)
            ])
            .Build();
        var services = new ServiceCollection()
            .ConfigureInfrastructure(config)
            .BuildServiceProvider();

        cache = services.GetRequiredService<ConnectionMultiplexer>();
        next = Substitute.For<IRepository>();
        service = services.GetRequiredService<ITransactionService>();
    }

    [Fact]
    public async Task Should_Save_Correctly()
    { 
        var accountId = 1;
        var transaction = new TransactionRequest(
            valor: 20,
            tipo: 'c',
            descricao: "a");

        var result = await service.TryExecuteTransactionAsync(accountId, transaction, CancellationToken.None);

        var actual = await service.GetBankStatementAsync(accountId);
    }
    //     actual.Saldo.Limite.Should().Be(transaction.Limite);
    //     actual.Saldo.Total.Should().Be(transaction.Saldo);
    // }

    // [Fact]
    // public async Task Should_Get_From_Next_When_Not_Found()
    // {
    //     var account = new Account(9999, 20000, 0);
    //     next.GetAccountByIdAsync(Arg.Is(account.Id)).Returns(account);

    //     var actual = await service.GetAccountByIdAsync(account.Id);

    //     actual.Should().BeEquivalentTo(account);
    //     await next.Received(1).GetAccountByIdAsync(Arg.Is(account.Id));
    // }

    // [Fact]
    // public async Task SavingCreditShouldDoAutomatically()
    // {
    //     // Given
    //     var transaction = new Transaction(250, Transaction.Credit, "deposito", 4, 0, 0, DateTime.Now);
    //     // When

    //     var result = await service.Save(transaction);
    
    //     // Then
    //     result.Should().BeTrue();
    // }

    // [Fact]
    // public async Task SavingCreditShouldIncrementAccountSnapshot()
    // {
    //     // Given
    //     var transaction = new Transaction(250, Transaction.Credit, "deposito", 4, 0, 0, DateTime.Now);

    //     // When
    //     await service.Save(transaction);

    //     var actual = await service.GetAccountByIdAsync(transaction.AccountId);
    
    //     // Then
    //     actual.Saldo.Should().Be(transaction.Valor);
    // }

    // [Fact]
    // public async Task SavingDebtShouldDecrementAccountSnapshot()
    // {
    //     var existingTransaction = new Transaction(250, Transaction.Credit, "deposito", 4, 0, 0, DateTime.Now);
    //     await service.Save(existingTransaction);

    //     var debtTransaction = new Transaction(existingTransaction.Valor,
    //         Transaction.Debt, "debito", existingTransaction.AccountId, 0, 0, DateTime.Now);

    //     await service.Save(debtTransaction);
    
    //     var expectedSaldo = debtTransaction.Valor - existingTransaction.Valor;
    //     var actual = await service.GetAccountByIdAsync(debtTransaction.AccountId);
    //     actual.Saldo.Should().Be(expectedSaldo);
    // }

    // [Fact]
    // public async Task Invalid_Debt_Transaction_Should_Fail()
    // {
    //     var accountId = 9;
    //     var existingTransaction = new Transaction(
    //         valor: 250,
    //         tipo: Transaction.Credit,
    //         descricao: "deposito",
    //         accountId: accountId,
    //         limite: 0,
    //         saldo: 0,
    //         realizadaEm: DateTime.Now);
            
    //     await service.Save(existingTransaction);

    //     var invalidDebtTransaction = new Transaction(
    //         valor: existingTransaction.Valor + 1,
    //         tipo: Transaction.Debt,
    //         descricao: "debito",
    //         accountId: accountId,
    //         limite: 0,
    //         saldo: 0,
    //         realizadaEm: DateTime.Now);

    //     var result = await service.Save(invalidDebtTransaction);
    
    //     result.Should().BeFalse();
    // }

    // [Fact]
    // public async Task And_Account_Is_Not_Found_Should_Fail()
    // {
    //     var inexistentId = 20;
    //     var invalidDebtTransaction = new Transaction(
    //         valor: 1,
    //         tipo: Transaction.Debt,
    //         descricao: "debito",
    //         accountId: inexistentId,
    //         limite: 0,
    //         saldo: 0,
    //         realizadaEm: DateTime.Now);

    //     var result = await service.Save(invalidDebtTransaction);
    
    //     result.Should().BeFalse();
    // }

    // [Fact]
    // public async Task Transaction_Should_Be_Updated_If_Occurs_With_Differente_Balance_Values()
    // {
    //     var account = new Account(9, limite: 0, saldo: 0);

    //     //Debt created
    //     TransactionRequest debtRequest = new TransactionRequest(
    //                 valor: account.Saldo + 1,
    //                 tipo: Transaction.Debt,
    //                 descricao: "alterValue");

    //     var debt = account.Execute(debtRequest);
        
    //     // Credit occurs before debt (concurrency)
    //     var credit = account.Execute(new TransactionRequest(
    //         valor: 250,
    //         tipo: Transaction.Credit,
    //         descricao: "deposito"));

    //     await service.Save(credit);

    //     //Debt is finally saved.
    //     await service.Save(debt);

    //     var actual = await service.GetBankStatementAsync(account.Id);
    
    //     var actualDebt = actual.UltimasTransacoes.First();
    //     var expectedSaldo = credit.Valor - debtRequest.Valor;
        
    //     //This ensures the maths match.
    //     actual.Saldo.Total.Should().Be(expectedSaldo);
        
    //     // This ensures database will be consistent when saved.
    //     actual.Saldo.Total.Should().Be(debt.Saldo);

    //     actualDebt.RealizadaEm.Should().Be(debt.RealizadaEm);
    // }

    public void Dispose()
    {
        _ = cache.GetDatabase().ExecuteAsync("FLUSHDB").GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
