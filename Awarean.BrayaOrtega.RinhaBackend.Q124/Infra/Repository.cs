using System.Data;
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
        var sql = @"SELECT Limite, Saldo FROM Transactions 
                    WHERE AccountId = @Id
                    ORDER BY RealizadaEm
                    LIMIT 1;";

        using var conn = await dataSource.OpenConnectionAsync();

        try
        {
            var account = await ReadAccountAsync(conn, sql, id);
            return account;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{nameof(GetBankStatementAsync)}: {ex.Message}");
            throw;
        }

        finally
        {
            await conn.CloseAsync();
        }
    }

    private static async Task<Account> ReadAccountAsync(NpgsqlConnection conn, string sql, int accountId)
    {
        using var command = conn.CreateCommand();
        var parameter = new NpgsqlParameter("@Id", NpgsqlDbType.Integer) { Value = accountId };
        command.CommandText = sql;
        command.Parameters.Add(parameter);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var limite = reader.GetInt32(0);
            var saldo = reader.GetInt32(1);

            return new Account(accountId, limite, saldo);
        }

        return new Account();
    }

    public async Task<BankStatement> GetBankStatementAsync(int id)
    {
        var sql = @$"SELECT Limite, Saldo, Descricao, RealizadaEm, Valor, Tipo 
            FROM Transactions
            WHERE AccountId = @Id
            ORDER BY RealizadaEm DESC
            LIMIT 10;";

        using var conn = await dataSource.OpenConnectionAsync();

        using var command = new NpgsqlCommand(sql, conn);
        command.Parameters.AddWithValue("@Id", id);

        try
        {
            var reader = await command.ExecuteReaderAsync();

            var bankStatement = CreateBankStatement(reader);
            return bankStatement;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{nameof(GetBankStatementAsync)}: {ex.Message}");
            throw;
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    private static BankStatement CreateBankStatement(NpgsqlDataReader reader)
    {
        Balance balance = null;
        List<BankStatementTransaction> transactions = [];

        while (reader.Read())
        {
            balance = ExtractAccountBalanceFromLastAddedSnapshot(reader, balance);
            BankStatementTransaction transaction = GetTransaction(reader);

            if (transaction is not null)
                transactions.Add(transaction);
        }

        if (balance is not null)
            return new BankStatement(balance, transactions);

        return null;
    }

    private static BankStatementTransaction GetTransaction(NpgsqlDataReader reader)
    {
        BankStatementTransaction transaction = null;
        try
        {
            var descricao = reader.IsDBNull("Descricao") ? null : reader.GetString(2);
            if (descricao is null)
                return null;

            var realizadaEm = reader.IsDBNull(3) ? default : reader.GetDateTime(3);
            if (realizadaEm == default)
                return transaction;

            var valor = reader.GetInt32(4);
            var tipo = reader.GetChar(5);

            transaction = new BankStatementTransaction(valor, tipo, descricao, realizadaEm);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        

        return transaction;
    }

    private static Balance ExtractAccountBalanceFromLastAddedSnapshot(NpgsqlDataReader reader, Balance balance)
    {
        if (balance is null)
        {
            var limite = reader.GetInt32(0);
            var saldo = reader.GetInt32(1);

            balance = new Balance(saldo, limite);
        }

        return balance;
    }

    public async Task<bool> Save(Transaction transaction)
    {
        var sql = @"INSERT INTO Transactions 
                            (Tipo, Valor, Descricao, RealizadaEm, Limite, Saldo, AccountId)
                    VALUES (@Tipo, @Valor, @Descricao, @RealizadaEm, @Limite, @Saldo, @Id);";

        using var conn = await dataSource.OpenConnectionAsync();

        var command = conn.CreateCommand();

        command.CommandText = sql;
        command.Parameters.AddWithValue("@Id", NpgsqlDbType.Integer, transaction.AccountId);
        command.Parameters.AddWithValue("@Limite", NpgsqlDbType.Bigint, transaction.Limite);
        command.Parameters.AddWithValue("@Saldo", NpgsqlDbType.Bigint, transaction.Saldo);
        command.Parameters.AddWithValue("@Tipo", NpgsqlDbType.Varchar, size: 1, transaction.Tipo);
        command.Parameters.AddWithValue("@Valor", NpgsqlDbType.Bigint, transaction.Valor);
        command.Parameters.AddWithValue("@Descricao", NpgsqlDbType.Varchar, transaction.Descricao);
        command.Parameters.AddWithValue("@RealizadaEm", NpgsqlDbType.Date, transaction.RealizadaEm);

        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{nameof(Save)}: {ex.Message}");
            throw;
        }
        finally
        {
            await conn.CloseAsync();
        }

        return true;
    }
}
