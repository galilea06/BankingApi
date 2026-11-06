using Banking.Application.Dtos;

namespace Banking.Application.Abstractions;

public interface ICustomerService
{
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
