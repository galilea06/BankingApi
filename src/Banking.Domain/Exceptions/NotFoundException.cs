namespace Banking.Domain.Exceptions;

/// <summary>El recurso solicitado no existe (→ 404).</summary>
public sealed class NotFoundException : BusinessException
{
    private NotFoundException(string message) : base("not_found", message) { }

    public static NotFoundException Customer(Guid id) =>
        new($"No se encontró el cliente con id '{id}'.");

    public static NotFoundException Account(string accountNumber) =>
        new($"No se encontró la cuenta '{accountNumber}'.");
}
