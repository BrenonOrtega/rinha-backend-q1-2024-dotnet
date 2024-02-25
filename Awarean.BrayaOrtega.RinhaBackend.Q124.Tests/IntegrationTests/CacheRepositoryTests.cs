
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using FluentAssertions;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Tests.IntegrationTests;

public class CacheRepositoryTests : IDisposable
{
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
            .ConfigureInfrastructure(config)
            .BuildServiceProvider();

        var cache = services.GetRequiredService<IDistributedCache>();
        next = Substitute.For<IRepository>();
        repo = new CacheRepository(cache, next);
    }

    [Fact]
    public async Task Should_Save_Correctly()
    {
        var transaction = new Transaction(20, 'c', "a", 1, 20000, 0, DateTime.UtcNow);
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

    public void Dispose()
    {
    }
}
