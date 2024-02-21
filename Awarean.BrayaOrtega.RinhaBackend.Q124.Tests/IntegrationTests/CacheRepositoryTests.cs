
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Configurations;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using NSubstitute;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using FluentAssertions;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Tests.IntegrationTests;

public class CacheRepositoryTests : IDisposable
{
    private readonly ConnectionMultiplexer mtp;
    private readonly IRepository next;
    private readonly IRepository repo;

    public CacheRepositoryTests()
    {
        const string connectionString = "Server=localhost;Port=5432;Database=rinha_database;User Id=postgres;Password=postgres;Include Error Detail=true;";
        const string redisString = "localhost:6379,password=redis,ssl=False";

        var services = new ServiceCollection()
            .ConfigureInfrastructure(connectionString, redisString)
            .BuildServiceProvider();

        mtp = services.GetRequiredService<ConnectionMultiplexer>();
        next = Substitute.For<IRepository>();
        repo = new CacheRepository(mtp, next);
    }

    [Fact]
    public async Task Should_Save_Correctly()
    {
        var account = new Account(1, 20000, 0);
        var transaction = new Transaction(20,'c', "a", account.Id);
        await repo.Save(account, transaction);

        var actual = await repo.GetAccountByIdAsync(account.Id);

        actual.Should().BeEquivalentTo(account);
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
        mtp.GetDatabase().Execute("FLUSHDB");
        mtp.Dispose();
    }
}
