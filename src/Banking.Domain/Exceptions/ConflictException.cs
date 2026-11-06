namespace Banking.Domain.Exceptions;

/// <summary>La operación choca con el estado actual del sistema (→ 409).</summary>
public sealed class ConflictException : BusinessException
{
    public ConflictException(string message) : base("conflict", message) { }
}
