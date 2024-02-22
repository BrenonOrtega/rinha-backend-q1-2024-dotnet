using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using FluentAssertions;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Tests.UnitTests;

public sealed class AccountTests
{
    readonly TransactionRequest invalidTransaction = new(3, 'd', "Debito que deixaria a conta com valor menor que o limite");

    [Theory]
    [MemberData(nameof(InvalidTransactionsGenerator))]
    public void Shouldnt_Allow_invalid_Transaction(int balance, TransactionRequest invalidTransaction)
    {
        var account = new Account(1, 10000, balance);

        account.CanExecute(invalidTransaction).Should().BeFalse();
    }
    
    public static IEnumerable<object[]> InvalidTransactionsGenerator()
    {
        var balance = -9998;
        yield return [balance, new TransactionRequest(3, 'e', "debito")];
        yield return [balance, new TransactionRequest(10, 'e', "debito")];
        yield return [balance, new TransactionRequest(1, ' ', "debito")];
        yield return [balance, new TransactionRequest(1, '0', "debito")];
        yield return [balance, new TransactionRequest(1, 'f', "debito")];
        yield return [balance, new TransactionRequest(1, 'g', "debito")];
        yield return [balance, new TransactionRequest(1, 'g', "")];
        yield return [balance, new TransactionRequest(1, 'g', null)];
        yield return [balance, new TransactionRequest(1, 'g', "STR 11 CHARS")];

    }

    [Theory]
    [MemberData(nameof(ValidTransactionsGenerator))]
    public void Should_Allow_Valid_Transaction(TransactionRequest transaction)
    {
        var account = new Account(1, 10000, -9998);

        account.CanExecute(transaction).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(ValidTransactionsGenerator))]
    public void Executed_Transactions_Should_Add_To_Account(TransactionRequest transaction)
    {
        var account = new Account(1, 10000, -9998);

        var actual = account.Execute(transaction);

        actual.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(ValidTransactionsGeneratorWithExpectedBalance))]
    public void Executing_Transactions_Should_Change_Balance(int accountBalance,
        int expectedBalance, TransactionRequest transaction)
    {
        var account = new Account(1, 10000, accountBalance);

        account.Execute(transaction);

        account.Saldo.Should().Be(expectedBalance);
    }

    public static IEnumerable<object[]> ValidTransactionsGenerator()
    {
        yield return [new TransactionRequest(3, 'c', "Credito")];
        // "Debito que deixa a conta no limite"
        yield return [new TransactionRequest(2, 'd', "Debito")];
        //debito que deixa a conta 1 centavo antes do limite"
        yield return [new TransactionRequest(1, 'd', "limite -1")];
        //Descricao 10 characteres
        yield return [new TransactionRequest(3, 'd', "0123456789")];
    }

    public static IEnumerable<object[]> ValidTransactionsGeneratorWithExpectedBalance()
    {
        var balance = -9998;
        yield return [balance, -9995, new TransactionRequest(3, 'c', "Credito em conta")];
        yield return [balance, -10000, new TransactionRequest(2, 'd', "fica limi.")];
        yield return [balance, -10000, new TransactionRequest(2, 'd', "")];
        yield return [balance, -10000, new TransactionRequest(2, 'd', "null")];
        yield return [balance, -9999, new TransactionRequest(1, 'd', "debito")];
    }
}