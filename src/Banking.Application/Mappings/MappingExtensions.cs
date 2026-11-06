using Banking.Application.Dtos;
using Banking.Domain.Entities;

namespace Banking.Application.Mappings;

internal static class MappingExtensions
{
    public static CustomerResponse ToResponse(this Customer c) =>
        new(c.Id, c.FullName, c.DateOfBirth, c.Gender, c.MonthlyIncome, c.CreatedAtUtc);

    public static AccountResponse ToResponse(this Account a) =>
        new(a.Id, a.AccountNumber, a.CustomerId, a.Balance, a.CreatedAtUtc);

    public static TransactionResponse ToResponse(this Transaction t) =>
        new(t.Id, t.Type, t.Amount, t.OccurredAtUtc, t.BalanceAfter);
}
