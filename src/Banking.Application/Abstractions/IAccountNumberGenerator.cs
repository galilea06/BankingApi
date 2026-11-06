namespace Banking.Application.Abstractions;

public interface IAccountNumberGenerator
{
    /// <summary>Genera un número con el formato ACC-YYYYMMDD-XXXX.</summary>
    string Generate();
}
