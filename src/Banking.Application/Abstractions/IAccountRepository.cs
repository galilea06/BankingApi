using Banking.Domain.Entities;

namespace Banking.Application.Abstractions;

public interface IAccountRepository
{
    Task AddAsync(Account account, CancellationToken cancellationToken = default);

    Task<Account?> GetByAccountNumberAsync(
        string accountNumber,
        bool includeTransactions = false,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string accountNumber, CancellationToken cancellationToken = default);
}
