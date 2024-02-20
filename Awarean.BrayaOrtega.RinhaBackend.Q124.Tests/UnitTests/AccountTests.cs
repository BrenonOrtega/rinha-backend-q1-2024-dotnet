using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using FluentAssertions;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Tests.UnitTests;

public sealed class AccountTests
{
    readonly TransactionRequest invalidTransaction = new (3, "d", "Debito que deixaria a conta com valor menor que o limite");

    [Fact]
    public void Shouldnt_Allow_invalid_Transaction()
    {
        var account = new Account(1, 10000, -9998);

        account.CanExecute(invalidTransaction).Should().BeFalse();
    }

    [Fact]
    public void Executing_invalid_Transaction_Should_Throw()
    {
        var account = new Account(1, 10000, -9998);

        var invalidAction = () => account.Execute(invalidTransaction);

        invalidAction.Should().Throw<InvalidOperationException>();
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
        yield return [ new TransactionRequest(3, "c", "Credito em conta") ];
        yield return [ new TransactionRequest(2, "d", "Debito que deixa a conta no limite") ];
        yield return [ new TransactionRequest(1, "d", "debito que deixa a conta 1 centavo antes do limite") ];
    }

    public static IEnumerable<object[]> ValidTransactionsGeneratorWithExpectedBalance()
    {
        var balance = -9998;
        yield return [ balance, -9995, new TransactionRequest(3, "c", "Credito em conta") ];
        yield return [ balance, -10000, new TransactionRequest(2, "d", "Debito que deixa a conta no limite") ];
        yield return [ balance, -9999, new TransactionRequest(1, "d", "debito que deixa a conta 1 centavo antes do limite") ];
    }
}