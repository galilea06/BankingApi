using Banking.Application.Abstractions;
using Banking.Application.Dtos;
using Banking.Application.Mappings;
using Banking.Domain.Entities;
using Banking.Domain.Exceptions;

namespace Banking.Application.Services;

public sealed class CustomerService(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICustomerService
{
    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = Customer.Create(
            request.FullName,
            request.DateOfBirth ?? default,
            request.Gender ?? default,
            request.MonthlyIncome ?? 0m,
            clock.GetUtcNow().UtcDateTime);

        await customers.AddAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.ToResponse();
    }

    public async Task<CustomerResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(id, cancellationToken)
                       ?? throw NotFoundException.Customer(id);

        return customer.ToResponse();
    }
}
