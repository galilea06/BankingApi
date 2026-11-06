using Banking.Application.Abstractions;
using Banking.Application.Dtos;
using Banking.Application.Services;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Banking.Tests.Fakes;

namespace Banking.Tests.Application;

public class AccountServiceTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero));
    private readonly InMemoryAccountRepository _accounts = new();
    private readonly InMemoryCustomerRepository _customers = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private AccountService CreateService(IAccountNumberGenerator? generator = null) =>
        new(_accounts, _customers, generator ?? new SequenceAccountNumberGenerator("ACC-20260618-0001"), _unitOfWork, _clock);

    private async Task<Customer> SeedCustomerAsync()
    {
        var customer = Customer.Create(
            "Ana Pérez", new DateOnly(1990, 5, 20), Gender.Female, 2000m, _clock.GetUtcNow().UtcDateTime);
        await _customers.AddAsync(customer);
        return customer;
    }

    private async Task<string> SeedAccountAsync(AccountService service, decimal initialBalance = 100m)
    {
        var customer = await SeedCustomerAsync();
        var account = await service.CreateAsync(new CreateAccountRequest
        {
            CustomerId = customer.Id,
            InitialBalance = initialBalance
        });
        return account.AccountNumber;
    }

    // ---------- Crear cuenta ----------

    [Fact]
    public async Task CreateAsync_ForExistingCustomer_CreatesAccountWithInitialBalanceAndGeneratedNumber()
    {
        var service = CreateService();
        var customer = await SeedCustomerAsync();

        var account = await service.CreateAsync(new CreateAccountRequest { CustomerId = customer.Id, InitialBalance = 500m });

        Assert.Equal("ACC-20260618-0001", account.AccountNumber);
        Assert.Equal(500m, account.Balance);
        Assert.Equal(customer.Id, account.CustomerId);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_ForUnknownCustomer_ThrowsNotFound()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateAsync(new CreateAccountRequest { CustomerId = Guid.NewGuid(), InitialBalance = 10m }));
    }

    [Fact]
    public async Task CreateAsync_WhenGeneratedNumberAlreadyExists_RetriesUntilUnique()
    {
        // La segunda cuenta primero recibe un número ya usado (colisión) y luego uno libre.
        var generator = new SequenceAccountNumberGenerator(
            "ACC-20260618-0001",
            "ACC-20260618-0001",
            "ACC-20260618-0002");
        var service = CreateService(generator);
        var customer = await SeedCustomerAsync();

        var first = await service.CreateAsync(new CreateAccountRequest { CustomerId = customer.Id, InitialBalance = 0m });
        var second = await service.CreateAsync(new CreateAccountRequest { CustomerId = customer.Id, InitialBalance = 0m });

        Assert.Equal("ACC-20260618-0001", first.AccountNumber);
        Assert.Equal("ACC-20260618-0002", second.AccountNumber);
    }

    [Fact]
    public async Task CreateAsync_WhenEveryAttemptCollides_ThrowsConflict()
    {
        var colliding = Enumerable.Repeat("ACC-20260618-0001", AccountService.MaxAccountNumberAttempts + 1).ToArray();
        var service = CreateService(new SequenceAccountNumberGenerator(colliding));
        var customer = await SeedCustomerAsync();

        await service.CreateAsync(new CreateAccountRequest { CustomerId = customer.Id, InitialBalance = 0m });

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateAccountRequest { CustomerId = customer.Id, InitialBalance = 0m }));
    }

    // ---------- Saldo ----------

    [Fact]
    public async Task GetBalanceAsync_ReturnsCurrentBalance()
    {
        var service = CreateService();
        var number = await SeedAccountAsync(service, 250m);

        var balance = await service.GetBalanceAsync(number);

        Assert.Equal(250m, balance.Balance);
        Assert.Equal(number, balance.AccountNumber);
    }

    [Fact]
    public async Task GetBalanceAsync_ForUnknownAccount_ThrowsNotFound()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetBalanceAsync("ACC-00000000-0000"));
    }

    // ---------- Depósitos y retiros ----------

    [Fact]
    public async Task DepositAsync_IncreasesBalanceAndPersists()
    {
        var service = CreateService();
        var number = await SeedAccountAsync(service, 100m);

        var transaction = await service.DepositAsync(number, 40m);

        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(140m, transaction.BalanceAfter);
        Assert.Equal(140m, (await service.GetBalanceAsync(number)).Balance);
        Assert.Equal(2, _unitOfWork.SaveCount); // crear cuenta + depósito
    }

    [Fact]
    public async Task WithdrawAsync_WithSufficientFunds_DecreasesBalance()
    {
        var service = CreateService();
        var number = await SeedAccountAsync(service, 100m);

        var transaction = await service.WithdrawAsync(number, 60m);

        Assert.Equal(TransactionType.Withdrawal, transaction.Type);
        Assert.Equal(40m, transaction.BalanceAfter);
    }

    [Fact]
    public async Task WithdrawAsync_WithInsufficientFunds_IsRejectedAndNothingIsSaved()
    {
        var service = CreateService();
        var number = await SeedAccountAsync(service, 100m);
        var savesBefore = _unitOfWork.SaveCount;

        await Assert.ThrowsAsync<InsufficientFundsException>(() => service.WithdrawAsync(number, 100.01m));

        Assert.Equal(100m, (await service.GetBalanceAsync(number)).Balance);
        Assert.Equal(savesBefore, _unitOfWork.SaveCount);
        Assert.Empty(await service.GetTransactionsAsync(number));
    }

    [Fact]
    public async Task DepositAsync_ForUnknownAccount_ThrowsNotFound()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.DepositAsync("ACC-00000000-0000", 10m));
    }

    // ---------- Historial ----------

    [Fact]
    public async Task GetTransactionsAsync_ReturnsMovementsInChronologicalOrderWithHistoricalBalance()
    {
        var service = CreateService();
        var number = await SeedAccountAsync(service, 100m);

        await service.DepositAsync(number, 50m);      // 150
        _clock.Advance(TimeSpan.FromMinutes(1));
        await service.WithdrawAsync(number, 20m);     // 130
        _clock.Advance(TimeSpan.FromMinutes(1));
        await service.DepositAsync(number, 5m);       // 135

        var history = await service.GetTransactionsAsync(number);

        Assert.Equal(3, history.Count);
        Assert.Equal(
            new[] { TransactionType.Deposit, TransactionType.Withdrawal, TransactionType.Deposit },
            history.Select(t => t.Type));
        Assert.Equal(new[] { 150m, 130m, 135m }, history.Select(t => t.BalanceAfter));
        Assert.Equal(history.OrderBy(t => t.TimestampUtc), history);
        Assert.Equal(3, history.Select(t => t.TransactionId).Distinct().Count());
    }

    [Fact]
    public async Task GetTransactionsAsync_ForUnknownAccount_ThrowsNotFound()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetTransactionsAsync("ACC-00000000-0000"));
    }
}
