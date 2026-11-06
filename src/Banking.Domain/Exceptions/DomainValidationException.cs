namespace Banking.Domain.Exceptions;

/// <summary>Una regla de negocio o validación de datos fue violada (→ 400).</summary>
public sealed class DomainValidationException : BusinessException
{
    public DomainValidationException(string message) : base("validation_error", message) { }
}
