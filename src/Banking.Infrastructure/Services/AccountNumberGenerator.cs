using Banking.Application.Abstractions;

namespace Banking.Infrastructure.Services;

/// <summary>
/// Genera números con el formato exacto <c>ACC-YYYYMMDD-XXXX</c>
/// (prefijo constante, fecha UTC de creación y 4 dígitos aleatorios).
/// La unicidad global NO la garantiza este generador (solo hay 10 000 valores por día):
/// la garantizan la verificación con reintentos del servicio y el índice único de la BD.
/// </summary>
public sealed class AccountNumberGenerator : IAccountNumberGenerator
{
    public const string Prefix = "ACC-";

    private readonly TimeProvider _clock;
    private readonly Random _random;

    /// <param name="clock">Fuente de tiempo (inyectable para pruebas).</param>
    /// <param name="random">Opcional: un <see cref="Random"/> con semilla para pruebas deterministas.</param>
    public AccountNumberGenerator(TimeProvider clock, Random? random = null)
    {
        _clock = clock;
        _random = random ?? Random.Shared;
    }

    public string Generate()
    {
        var today = _clock.GetUtcNow().UtcDateTime;
        var suffix = _random.Next(0, 10_000);

        return $"{Prefix}{today:yyyyMMdd}-{suffix:D4}";
    }
}
