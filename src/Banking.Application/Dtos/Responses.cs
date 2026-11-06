using Banking.Domain.Enums;

namespace Banking.Application.Dtos;

public sealed record CustomerResponse(
    Guid Id,
    string FullName,
    DateOnly DateOfBirth,
    Gender Gender,
    decimal MonthlyIncome,
    DateTime CreatedAtUtc);

public sealed record AccountResponse(
    Guid Id,
    string AccountNumber,
    Guid CustomerId,
    decimal Balance,
    DateTime CreatedAtUtc);

public sealed record BalanceResponse(
    string AccountNumber,
    decimal Balance);

public sealed record TransactionResponse(
    Guid TransactionId,
    TransactionType Type,
    decimal Amount,
    DateTime TimestampUtc,
    decimal BalanceAfter);
