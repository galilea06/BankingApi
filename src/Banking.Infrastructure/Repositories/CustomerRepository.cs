using Banking.Application.Abstractions;
using Banking.Domain.Entities;
using Banking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Repositories;

internal sealed class CustomerRepository(BankingDbContext context) : ICustomerRepository
{
    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default) =>
        await context.Customers.AddAsync(customer, cancellationToken);

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
}
