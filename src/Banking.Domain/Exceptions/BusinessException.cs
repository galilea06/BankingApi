namespace Banking.Domain.Exceptions;

/// <summary>
/// Base de todas las excepciones de negocio. El manejador global las captura
/// y las traduce a una respuesta HTTP limpia (el dominio no conoce HTTP).
/// </summary>
public abstract class BusinessException : Exception
{
    protected BusinessException(string code, string message) : base(message)
    {
        Code = code;
    }

    /// <summary>Código estable y legible por máquina (útil para clientes de la API).</summary>
    public string Code { get; }
}
