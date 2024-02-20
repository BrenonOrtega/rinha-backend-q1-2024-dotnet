using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using Npgsql;
using Dapper;
using System.Data;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;

public class Repository
{
    private readonly NpgsqlDataSource dataSource;

    public Repository(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task<Account> GetAccountByIdAsync(int id)
    {
        var sql = "SELECT Id, Limite, Saldo FROM accounts WHERE Id = @Id";
        var parameters = new DynamicParameters();

        parameters.Add("@Id", id, DbType.Int32);

        var conn = dataSource.CreateConnection();

        var account = await conn.QueryFirstAsync<Account>(sql, parameters);

        return account;
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

        using var command = new NpgsqlCommand(sql, conn);
        command.Parameters.AddWithValue("@Id", id);

        var reader = await command.ExecuteReaderAsync();

        Balance? balance = null;
        List<BankStatementTransaction> transactions = new();
        while (reader.Read())
        {
            if (balance == null)
            {
                var limite = reader.GetInt32(0);
                var saldo = reader.GetInt32(1);

                balance = new Balance(saldo, limite);
            }

            // Retrieve data from the reader
            var realizadaEm = reader.GetDateTime(2);
            var valor = reader.GetInt32(3);
            var tipo = reader.GetString(4);
            var descricao = reader.GetString(5);

            transactions.Add(new BankStatementTransaction(valor, tipo, descricao, realizadaEm));

        }
        return new BankStatement(balance.Value, transactions);
    }

    public async Task Save(Account account, Transaction transaction)
    {
        var sql = @"BEGIN;
                    UPDATE Accounts SET Limite = @Limite, Saldo = @Saldo WHERE Id = @Id;
                    INSERT INTO Transactions (Tipo, Valor, Descricao, RealizadaEm, AccountId)
                    VALUES (@TIPO, @Valor, @Descricao, now(), @Id);
                    COMMIT";

        var parameters = new DynamicParameters();

        parameters.Add("@Id", account.Id, DbType.Int32);
        parameters.Add("@Limite", account.Saldo, DbType.Int32);
        parameters.Add("@Saldo", account.Limite, DbType.Int32);

        parameters.Add("@Tipo", transaction.Tipo, DbType.StringFixedLength, size: 1);
        parameters.Add("@Valor", transaction.Valor, DbType.Int64);
        parameters.Add("@Descricao", transaction.Descricao, DbType.String, size: 10);

        var conn = dataSource.CreateConnection();

        var affectedRows = await conn.ExecuteAsync(sql, parameters);

        if (affectedRows != 2)
            throw new InvalidOperationException();
    }
}
