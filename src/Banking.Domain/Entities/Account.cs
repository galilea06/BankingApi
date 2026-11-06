using Banking.Domain.Enums;
using Banking.Domain.Exceptions;

namespace Banking.Domain.Entities;

/// <summary>
/// Agregado raíz. Todas las reglas de saldo viven aquí, de modo que ninguna
/// capa externa pueda dejar la cuenta en un estado inválido.
/// </summary>
public class Account
{
    private readonly List<Transaction> _transactions = new();

    private Account()
    {
        AccountNumber = string.Empty;
    }

    public Guid Id { get; private set; }
    public string AccountNumber { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal Balance { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Token de concurrencia optimista: evita "lost updates" en saldos.</summary>
    public int Version { get; private set; }

    public IReadOnlyCollection<Transaction> Transactions => _transactions;

    public static Account Open(Guid customerId, string accountNumber, decimal initialBalance, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new DomainValidationException("El número de cuenta es obligatorio.");

        if (initialBalance < 0)
            throw new DomainValidationException("El saldo inicial no puede ser negativo.");

        EnsureTwoDecimals(initialBalance, "El saldo inicial");

        return new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = accountNumber,
            CustomerId = customerId,
            Balance = initialBalance,
            CreatedAtUtc = nowUtc
        };
    }

    public Transaction Deposit(decimal amount, DateTime nowUtc)
    {
        ValidateAmount(amount);
        return Apply(TransactionType.Deposit, amount, Balance + amount, nowUtc);
    }

    public Transaction Withdraw(decimal amount, DateTime nowUtc)
    {
        ValidateAmount(amount);

        if (amount > Balance)
            throw new InsufficientFundsException();

        return Apply(TransactionType.Withdrawal, amount, Balance - amount, nowUtc);
    }

    private Transaction Apply(TransactionType type, decimal amount, decimal newBalance, DateTime nowUtc)
    {
        Balance = newBalance;
        Version++;

        var transaction = new Transaction(Id, type, amount, newBalance, nowUtc);
        _transactions.Add(transaction);
        return transaction;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new DomainValidationException("El monto debe ser mayor que cero.");

        EnsureTwoDecimals(amount, "El monto");
    }

    private static void EnsureTwoDecimals(decimal value, string label)
    {
        if (decimal.Round(value, 2) != value)
            throw new DomainValidationException($"{label} no puede tener más de 2 decimales.");
    }
}
