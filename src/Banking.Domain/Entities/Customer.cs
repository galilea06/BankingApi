using Banking.Domain.Enums;
using Banking.Domain.Exceptions;

namespace Banking.Domain.Entities;

public class Customer
{
    public const int MinimumAge = 18;
    public const int FullNameMaxLength = 150;

    private Customer()
    {
        FullName = string.Empty;
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public decimal MonthlyIncome { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Customer Create(
        string fullName,
        DateOnly dateOfBirth,
        Gender gender,
        decimal monthlyIncome,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainValidationException("El nombre completo es obligatorio.");

        fullName = fullName.Trim();
        if (fullName.Length > FullNameMaxLength)
            throw new DomainValidationException(
                $"El nombre completo no puede exceder {FullNameMaxLength} caracteres.");

        if (!Enum.IsDefined(gender))
            throw new DomainValidationException("El sexo/género indicado no es válido.");

        if (monthlyIncome < 0)
            throw new DomainValidationException("Los ingresos mensuales no pueden ser negativos.");

        var today = DateOnly.FromDateTime(nowUtc);
        if (dateOfBirth > today)
            throw new DomainValidationException("La fecha de nacimiento no puede estar en el futuro.");

        if (CalculateAge(dateOfBirth, today) < MinimumAge)
            throw new DomainValidationException(
                $"El cliente debe tener al menos {MinimumAge} años.");

        return new Customer
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            MonthlyIncome = decimal.Round(monthlyIncome, 2),
            CreatedAtUtc = nowUtc
        };
    }

    private static int CalculateAge(DateOnly birth, DateOnly today)
    {
        var age = today.Year - birth.Year;
        if (birth > today.AddYears(-age)) age--;
        return age;
    }
}
