using Banking.Application.Abstractions;
using Banking.Domain.Entities;

namespace Banking.Tests.Fakes;

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly Dictionary<Guid, Customer> _store = new();

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _store[customer.Id] = customer;
        return Task.CompletedTask;
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));
}

public sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly Dictionary<string, Account> _store = new();

    public int Count => _store.Count;

    public Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        _store[account.AccountNumber] = account;
        return Task.CompletedTask;
    }

    public Task<Account?> GetByAccountNumberAsync(
        string accountNumber,
        bool includeTransactions = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(accountNumber));

    public Task<bool> ExistsAsync(string accountNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.ContainsKey(accountNumber));
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

/// <summary>Devuelve una secuencia prefijada de números (para simular colisiones).</summary>
public sealed class SequenceAccountNumberGenerator(params string[] numbers) : IAccountNumberGenerator
{
    private readonly Queue<string> _queue = new(numbers);

    public string Generate() => _queue.Dequeue();
}
