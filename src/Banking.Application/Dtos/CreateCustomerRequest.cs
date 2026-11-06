using System.ComponentModel.DataAnnotations;
using Banking.Domain.Enums;

namespace Banking.Application.Dtos;

public sealed record CreateCustomerRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    public DateOnly? DateOfBirth { get; init; }

    [Required]
    public Gender? Gender { get; init; }

    [Required, Range(typeof(decimal), "0", "1000000000000")]
    public decimal? MonthlyIncome { get; init; }
}
