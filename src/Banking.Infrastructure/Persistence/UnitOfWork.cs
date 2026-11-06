using Banking.Application.Abstractions;
using Banking.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence;

internal sealed class UnitOfWork(BankingDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Dos operaciones simultáneas sobre la misma cuenta: la segunda debe reintentarse.
            throw new ConflictException(
                "La cuenta fue modificada por otra operación. Intente nuevamente.");
        }
    }
}
