using Banking.Domain.Enums;

namespace Banking.Domain.Entities;

/// <summary>
/// Movimiento inmutable de una cuenta. Guarda el saldo resultante (saldo histórico)
/// para poder reconstruir el estado de la cuenta en cualquier punto del tiempo.
/// </summary>
public class Transaction
{
    private Transaction() { }

    internal Transaction(
        Guid accountId,
        TransactionType type,
        decimal amount,
        decimal balanceAfter,
        DateTime occurredAtUtc)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        Type = type;
        Amount = amount;
        BalanceAfter = balanceAfter;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
}
