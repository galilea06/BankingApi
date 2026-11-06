using Banking.Application.Dtos;

namespace Banking.Application.Abstractions;

public interface IAccountService
{
    Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken = default);
    Task<BalanceResponse> GetBalanceAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task<TransactionResponse> DepositAsync(string accountNumber, decimal amount, CancellationToken cancellationToken = default);
    Task<TransactionResponse> WithdrawAsync(string accountNumber, decimal amount, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransactionResponse>> GetTransactionsAsync(string accountNumber, CancellationToken cancellationToken = default);
}
