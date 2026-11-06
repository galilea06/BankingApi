using Banking.Application.Abstractions;
using Banking.Infrastructure.Persistence;
using Banking.Infrastructure.Repositories;
using Banking.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Banking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Banking") ?? "Data Source=banking.db";

        services.AddDbContext<BankingDbContext>(options => options.UseSqlite(connectionString));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IAccountNumberGenerator, AccountNumberGenerator>();

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>Crea la base de datos SQLite (y sus índices) si aún no existe.</summary>
    public static void EnsureDatabaseCreated(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<BankingDbContext>().Database.EnsureCreated();
    }
}
