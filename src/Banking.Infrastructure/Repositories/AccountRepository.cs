using Banking.Application.Abstractions;
using Banking.Domain.Entities;
using Banking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Repositories;

internal sealed class AccountRepository(BankingDbContext context) : IAccountRepository
{
    public async Task AddAsync(Account account, CancellationToken cancellationToken = default) =>
        await context.Accounts.AddAsync(account, cancellationToken);

    public Task<Account?> GetByAccountNumberAsync(
        string accountNumber,
        bool includeTransactions = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Account> query = context.Accounts;

        if (includeTransactions)
            query = query.Include(a => a.Transactions);

        return query.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);
    }

    public Task<bool> ExistsAsync(string accountNumber, CancellationToken cancellationToken = default) =>
        context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber, cancellationToken);
}
