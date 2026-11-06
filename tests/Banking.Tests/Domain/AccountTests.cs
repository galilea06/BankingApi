using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;

namespace Banking.Tests.Domain;

public class AccountTests
{
    private static readonly DateTime Now = new(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);

    private static Account OpenAccount(decimal initialBalance = 100m) =>
        Account.Open(Guid.NewGuid(), "ACC-20260618-0001", initialBalance, Now);

    // ---------- Apertura ----------

    [Fact]
    public void Open_StartsWithInitialBalance()
    {
        var account = OpenAccount(250m);

        Assert.Equal(250m, account.Balance);
        Assert.Empty(account.Transactions);
    }

    [Fact]
    public void Open_WithNegativeInitialBalance_Throws()
    {
        Assert.Throws<DomainValidationException>(() => OpenAccount(-1m));
    }

    // ---------- Depósitos ----------

    [Fact]
    public void Deposit_IncreasesBalance()
    {
        var account = OpenAccount(100m);

        account.Deposit(50m, Now);

        Assert.Equal(150m, account.Balance);
    }

    [Fact]
    public void Deposit_RecordsTransactionWithResultingBalance()
    {
        var account = OpenAccount(100m);

        var transaction = account.Deposit(50m, Now);

        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(50m, transaction.Amount);
        Assert.Equal(150m, transaction.BalanceAfter);
        Assert.Equal(Now, transaction.OccurredAtUtc);
        Assert.NotEqual(Guid.Empty, transaction.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Deposit_WithNonPositiveAmount_Throws(double amount)
    {
        var account = OpenAccount();

        Assert.Throws<DomainValidationException>(() => account.Deposit((decimal)amount, Now));
        Assert.Equal(100m, account.Balance);
    }

    [Fact]
    public void Deposit_WithMoreThanTwoDecimals_Throws()
    {
        var account = OpenAccount();

        Assert.Throws<DomainValidationException>(() => account.Deposit(10.123m, Now));
    }

    // ---------- Retiros ----------

    [Fact]
    public void Withdraw_WithSufficientFunds_DecreasesBalance()
    {
        var account = OpenAccount(100m);

        var transaction = account.Withdraw(30m, Now);

        Assert.Equal(70m, account.Balance);
        Assert.Equal(TransactionType.Withdrawal, transaction.Type);
        Assert.Equal(70m, transaction.BalanceAfter);
    }

    [Fact]
    public void Withdraw_OfEntireBalance_IsAllowedAndLeavesZero()
    {
        var account = OpenAccount(100m);

        account.Withdraw(100m, Now);

        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public void Withdraw_WithInsufficientFunds_ThrowsAndLeavesAccountUntouched()
    {
        var account = OpenAccount(100m);

        Assert.Throws<InsufficientFundsException>(() => account.Withdraw(100.01m, Now));

        Assert.Equal(100m, account.Balance);
        Assert.Empty(account.Transactions);
    }

    [Fact]
    public void Withdraw_WithNonPositiveAmount_Throws()
    {
        var account = OpenAccount();

        Assert.Throws<DomainValidationException>(() => account.Withdraw(0m, Now));
    }

    // ---------- Saldo histórico ----------

    [Fact]
    public void Transactions_KeepTheHistoricalBalanceAfterEachMovement()
    {
        var account = OpenAccount(100m);

        account.Deposit(50m, Now);     // 150
        account.Withdraw(30m, Now);    // 120
        account.Deposit(10m, Now);     // 130

        Assert.Equal(new[] { 150m, 120m, 130m }, account.Transactions.Select(t => t.BalanceAfter));
    }
}
