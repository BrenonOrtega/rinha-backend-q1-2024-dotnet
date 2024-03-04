using System.Text.Json;
using Npgsql;
using StackExchange.Redis;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public class InitializeRedisBackgroundService : BackgroundService
{
    private readonly NpgsqlDataSource pg;
    private readonly ConnectionMultiplexer mp;
    private static readonly JsonSerializerOptions options;

    public InitializeRedisBackgroundService(NpgsqlDataSource pg, ConnectionMultiplexer mp)
    {
        this.pg = pg ?? throw new ArgumentNullException(nameof(pg));
        this.mp = mp ?? throw new ArgumentNullException(nameof(mp));
    }

    static InitializeRedisBackgroundService()
    {
        options = new JsonSerializerOptions();
        options.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var conn = GetConnectionAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Limite, Saldo, AccountId, RealizadaEm FROM Transactions WHERE Descricao IS NULL";

        var reader = cmd.ExecuteReaderAsync(stoppingToken).ConfigureAwait(false).GetAwaiter().GetResult();

        var redis = mp.GetDatabase();

        var tasks = new List<Task>();
        while (reader.ReadAsync(stoppingToken).ConfigureAwait(false).GetAwaiter().GetResult())
        {
            var limite = reader.GetInt32(0);
            var saldo = reader.GetInt32(1);
            var accountId = reader.GetInt32(2);
            var realizadaEm = reader.GetDateTime(3);

            var transaction = new Transaction(0, 'c', "", accountId, limite, saldo, realizadaEm);

            var json = JsonSerializer.Serialize(transaction, options);
            tasks.Add(redis.SortedSetAddAsync($"BankStatement:{accountId}", json, transaction.RealizadaEm.Ticks));
        }

        Task.WhenAll(tasks).ConfigureAwait(false).GetAwaiter().GetResult();

        conn.CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        return Task.CompletedTask;
    }

    private async Task<NpgsqlConnection> GetConnectionAsync()
    {
        try
        {
            return await pg.OpenConnectionAsync();
        }
        catch
        {
            await Task.Delay(150);
            return await GetConnectionAsync();
        }
    }

}