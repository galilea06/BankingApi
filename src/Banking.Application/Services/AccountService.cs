using Banking.Application.Abstractions;
using Banking.Application.Dtos;
using Banking.Application.Mappings;
using Banking.Domain.Entities;
using Banking.Domain.Exceptions;

namespace Banking.Application.Services;

public sealed class AccountService(
    IAccountRepository accounts,
    ICustomerRepository customers,
    IAccountNumberGenerator accountNumberGenerator,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IAccountService
{
    /// <summary>Con solo 10 000 combinaciones por día, se reintenta ante colisiones.</summary>
    public const int MaxAccountNumberAttempts = 10;

    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var customerId = request.CustomerId ?? Guid.Empty;

        _ = await customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw NotFoundException.Customer(customerId);

        var accountNumber = await GenerateUniqueAccountNumberAsync(cancellationToken);

        var account = Account.Open(
            customerId,
            accountNumber,
            request.InitialBalance ?? 0m,
            Now());

        await accounts.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return account.ToResponse();
    }

    public async Task<BalanceResponse> GetBalanceAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        var account = await GetAccountOrThrowAsync(accountNumber, includeTransactions: false, cancellationToken);
        return new BalanceResponse(account.AccountNumber, account.Balance);
    }

    public async Task<TransactionResponse> DepositAsync(string accountNumber, decimal amount, CancellationToken cancellationToken = default)
    {
        var account = await GetAccountOrThrowAsync(accountNumber, includeTransactions: false, cancellationToken);

        var transaction = account.Deposit(amount, Now());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction.ToResponse();
    }

    public async Task<TransactionResponse> WithdrawAsync(string accountNumber, decimal amount, CancellationToken cancellationToken = default)
    {
        var account = await GetAccountOrThrowAsync(accountNumber, includeTransactions: false, cancellationToken);

        var transaction = account.Withdraw(amount, Now());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return transaction.ToResponse();
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetTransactionsAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        var account = await GetAccountOrThrowAsync(accountNumber, includeTransactions: true, cancellationToken);

        // OrderBy es estable: ante marcas de tiempo idénticas se respeta el orden de inserción.
        return account.Transactions
            .OrderBy(t => t.OccurredAtUtc)
            .Select(t => t.ToResponse())
            .ToList();
    }

    private async Task<Account> GetAccountOrThrowAsync(string accountNumber, bool includeTransactions, CancellationToken cancellationToken)
    {
        var normalized = accountNumber?.Trim().ToUpperInvariant() ?? string.Empty;

        return await accounts.GetByAccountNumberAsync(normalized, includeTransactions, cancellationToken)
               ?? throw NotFoundException.Account(accountNumber ?? string.Empty);
    }

    private async Task<string> GenerateUniqueAccountNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAccountNumberAttempts; attempt++)
        {
            var candidate = accountNumberGenerator.Generate();

            if (!await accounts.ExistsAsync(candidate, cancellationToken))
                return candidate;
        }

        throw new ConflictException(
            "No fue posible generar un número de cuenta único. Intente nuevamente.");
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
}
