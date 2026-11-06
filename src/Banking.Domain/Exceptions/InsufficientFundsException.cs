namespace Banking.Domain.Exceptions;

/// <summary>Se intentó retirar más dinero del disponible (→ 400).</summary>
public sealed class InsufficientFundsException : BusinessException
{
    public InsufficientFundsException()
        : base("insufficient_funds", "Fondos insuficientes para realizar el retiro.") { }
}
