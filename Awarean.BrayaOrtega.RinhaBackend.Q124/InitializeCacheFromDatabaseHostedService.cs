
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Npgsql;
using StackExchange.Redis;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public class InitializeCacheFromDatabaseHostedService : BackgroundService
{
    private readonly ConnectionMultiplexer connection;
    private readonly NpgsqlDataSource pg;

    public InitializeCacheFromDatabaseHostedService(ConnectionMultiplexer connection, NpgsqlDataSource pg)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        this.pg = pg ?? throw new ArgumentNullException(nameof(pg));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var conn = await GetConnectionAsync();

        var existingIds = GetExistingAccountsAsync(conn);

        await foreach(var id in existingIds)
        {

        }

        var bankStatement = await Repository.GetBankStatementCoreAsync()
    }

    private static async IAsyncEnumerable<int> GetExistingAccountsAsync(NpgsqlConnection conn)
    {
        var command = conn.CreateCommand();
        command.CommandText = "SELECT DISTINCT AccountId FROM transactions WHERE Descricao IS NULL;";

        var reader = await command.ExecuteReaderAsync();

        while (reader.Read())
            yield return reader.GetInt32(0);
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