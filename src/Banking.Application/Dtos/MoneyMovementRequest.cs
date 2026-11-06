using System.ComponentModel.DataAnnotations;

namespace Banking.Application.Dtos;

/// <summary>Cuerpo compartido por depósitos y retiros.</summary>
public sealed record MoneyMovementRequest
{
    [Required, Range(typeof(decimal), "0.01", "1000000000000")]
    public decimal? Amount { get; init; }
}
