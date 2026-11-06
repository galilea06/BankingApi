using System.ComponentModel.DataAnnotations;

namespace Banking.Application.Dtos;

public sealed record CreateAccountRequest
{
    [Required]
    public Guid? CustomerId { get; init; }

    [Required, Range(typeof(decimal), "0", "1000000000000")]
    public decimal? InitialBalance { get; init; }
}
