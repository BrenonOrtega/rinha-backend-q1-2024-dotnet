using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using Npgsql;
using NpgsqlTypes;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;

public class Repository : IRepository
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
        await conn.CloseAsync();
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

            await conn.CloseAsync();

            return new Account(id, limite, saldo);
        }

        await conn.CloseAsync();

        return new Account();
    }

    public async Task<BankStatement> GetBankStatementAsync(int id)
    {
        var sql = @$"SELECT Limite, Saldo, RealizadaEm, Valor, Tipo, Descricao
            FROM Accounts a 
            LEFT JOIN Transactions t ON t.AccountId = a.Id
            WHERE a.Id = @Id
            ORDER BY RealizadaEm DESC
            LIMIT 10;";

        using var conn = dataSource.CreateConnection();

        using var command = new NpgsqlCommand(sql, conn);
        command.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();

        var reader = await command.ExecuteReaderAsync();

        var bankStatement = CreateBankStatement(reader);

        await conn.CloseAsync();

        return bankStatement;
    }

    private static BankStatement CreateBankStatement(NpgsqlDataReader reader)
    {
        Balance balance = new();
        List<BankStatementTransaction> transactions = [];

        while (reader.Read())
        {
            if (balance.IsEmpty())
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

        if (!balance.IsEmpty())
            return new BankStatement(balance, transactions);

        return new BankStatement();
    }

    public async Task Save(Account account, Transaction transaction)
    {
        var sql = @"BEGIN;
                    UPDATE Accounts SET Limite = @Limite, Saldo = @Saldo WHERE Id = @Id;
                    INSERT INTO Transactions (Tipo, Valor, Descricao, RealizadaEm, AccountId)
                    VALUES (@Tipo, @Valor, @Descricao, now(), @Id);
                    COMMIT";

        using var conn = dataSource.CreateConnection();

        var command = conn.CreateCommand();

        command.CommandText = sql;
        command.Parameters.AddWithValue("@Id", NpgsqlDbType.Integer, account.Id);
        command.Parameters.AddWithValue("@Limite", NpgsqlDbType.Bigint, account.Limite);
        command.Parameters.AddWithValue("@Saldo", NpgsqlDbType.Bigint, account.Saldo);
        command.Parameters.AddWithValue("@Tipo", NpgsqlDbType.Varchar, transaction.Tipo);
        command.Parameters.AddWithValue("@Valor", NpgsqlDbType.Bigint, transaction.Valor);
        command.Parameters.AddWithValue("@Descricao", NpgsqlDbType.Varchar, transaction.Descricao);

        await conn.OpenAsync();

        var affectedRows = await command.ExecuteNonQueryAsync();

        await conn.CloseAsync();

        if (affectedRows != 2)
            throw new InvalidOperationException();
    }
}
