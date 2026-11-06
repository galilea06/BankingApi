using Banking.Application.Dtos;
using Banking.Application.Services;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Banking.Tests.Fakes;

namespace Banking.Tests.Application;

public class CustomerServiceTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero));
    private readonly InMemoryCustomerRepository _customers = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private CustomerService CreateService() => new(_customers, _unitOfWork, _clock);

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesCustomer()
    {
        var service = CreateService();

        var customer = await service.CreateAsync(new CreateCustomerRequest
        {
            FullName = "  Ana Pérez  ",
            DateOfBirth = new DateOnly(1990, 5, 20),
            Gender = Gender.Female,
            MonthlyIncome = 1500.5m
        });

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal("Ana Pérez", customer.FullName);
        Assert.Equal(1500.5m, customer.MonthlyIncome);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_WithFutureBirthDate_ThrowsValidation()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateAsync(new CreateCustomerRequest
        {
            FullName = "Ana Pérez",
            DateOfBirth = new DateOnly(2030, 1, 1),
            Gender = Gender.Female,
            MonthlyIncome = 100m
        }));
    }

    [Fact]
    public async Task CreateAsync_ForMinor_ThrowsValidation()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateAsync(new CreateCustomerRequest
        {
            FullName = "Juan Menor",
            DateOfBirth = new DateOnly(2015, 1, 1),
            Gender = Gender.Male,
            MonthlyIncome = 0m
        }));
    }

    [Fact]
    public async Task GetByIdAsync_ForUnknownCustomer_ThrowsNotFound()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }
}
