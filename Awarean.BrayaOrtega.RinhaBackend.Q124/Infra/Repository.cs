using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using Npgsql;
using NpgsqlTypes;
using System.Data;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;

public sealed class Repository
{
    private readonly NpgsqlDataSource dataSource;

    public Repository(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task<Account> GetAccountByIdAsync(int id)
    {
        var sql = "SELECT Id, Limite, Saldo FROM accounts WHERE Id = @Id";

        using var conn = dataSource.CreateConnection();
        var parameters = new NpgsqlParameter("@Id", NpgsqlDbType.Integer) { Value = id };

        await conn.OpenAsync();
        var account = await ReadAccountAsync(conn, sql, parameters);

        return account;
    }

    private static async Task<Account> ReadAccountAsync(NpgsqlConnection conn, string sql, NpgsqlParameter parameter)
    {
        using var command = conn.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(parameter);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var id = reader.GetInt32(0);
            var limite = reader.GetInt64(1);
            var saldo = reader.GetInt64(2);

            return new Account(id, limite, saldo);
        }

        return null;
    }

    public async Task<BankStatement?> GetBankStatementAsync(int id)
    {
        // Don't do this if your not using a strongly typed language and 
        // even more if you're using strings as parameters
        var sql = @$"SELECT Limite, Saldo, RealizadaEm, Valor, Tipo, Descricao
            FROM Accounts a 
            LEFT JOIN Transactions t ON t.AccountId = a.Id
            WHERE a.Id = @Id;";

        using var conn = dataSource.CreateConnection();

        await conn.OpenAsync();

        using var command = new NpgsqlCommand(sql, conn);
        command.Parameters.AddWithValue("@Id", id);

        var reader = await command.ExecuteReaderAsync();

        Balance? balance = null;
        List<BankStatementTransaction> transactions = new();
        while (reader.Read())
        {
            if (balance == null)
            {
                var limite = reader.GetInt64(0);
                var saldo = reader.GetInt64(1);

                balance = new Balance(saldo, limite);
            }

            try
            {
                var realizadaEm = reader.IsDBNull(2) ? default : reader.GetDateTime(2);
                if (realizadaEm == default)
                    continue;

                var valor = reader.GetInt64(3);
                var tipo = reader.GetChar(4);
                var descricao = reader.GetString(5);

                transactions.Add(new BankStatementTransaction(valor, tipo, descricao, realizadaEm));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        if (balance.HasValue)
            return new BankStatement(balance.Value, transactions);
        
        return null;
    }

    public async Task Save(Account account, Transaction transaction)
    {
        var sql = @"BEGIN;
                    UPDATE Accounts SET Limite = @Limite, Saldo = @Saldo WHERE Id = @Id;
                    INSERT INTO Transactions (Tipo, Valor, Descricao, RealizadaEm, AccountId)
                    VALUES (@Tipo, @Valor, @Descricao, now(), @Id);
                    COMMIT";

        using var conn = dataSource.CreateConnection();
        await conn.OpenAsync();

        var command = conn.CreateCommand();

        command.CommandText = sql;
        command.Parameters.AddWithValue("@Id", NpgsqlDbType.Integer, account.Id);
        command.Parameters.AddWithValue("@Limite", NpgsqlDbType.Bigint, account.Limite);
        command.Parameters.AddWithValue("@Saldo", NpgsqlDbType.Bigint, account.Saldo);
        command.Parameters.AddWithValue("@Tipo", NpgsqlDbType.Varchar, transaction.Tipo);
        command.Parameters.AddWithValue("@Valor", NpgsqlDbType.Bigint, transaction.Valor);
        command.Parameters.AddWithValue("@Descricao", NpgsqlDbType.Varchar, transaction.Descricao);

        var affectedRows = await command.ExecuteNonQueryAsync();

        if (affectedRows != 2)
            throw new InvalidOperationException();
    }
}
