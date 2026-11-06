using Banking.Application.Abstractions;
using Banking.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController(IAccountService accountService) : ControllerBase
{
    /// <summary>Crea una cuenta para un cliente existente con un saldo inicial.</summary>
    [HttpPost]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponse>> Create(
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await accountService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBalance), new { accountNumber = account.AccountNumber }, account);
    }

    /// <summary>Consulta el saldo actual de una cuenta.</summary>
    [HttpGet("{accountNumber}/balance")]
    [ProducesResponseType<BalanceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BalanceResponse>> GetBalance(string accountNumber, CancellationToken cancellationToken) =>
        Ok(await accountService.GetBalanceAsync(accountNumber, cancellationToken));

    /// <summary>Registra un depósito.</summary>
    [HttpPost("{accountNumber}/deposits")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> Deposit(
        string accountNumber,
        MoneyMovementRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await accountService.DepositAsync(accountNumber, request.Amount ?? 0m, cancellationToken);
        return CreatedAtAction(nameof(GetTransactions), new { accountNumber }, transaction);
    }

    /// <summary>Registra un retiro (rechazado si no hay fondos suficientes).</summary>
    [HttpPost("{accountNumber}/withdrawals")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> Withdraw(
        string accountNumber,
        MoneyMovementRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await accountService.WithdrawAsync(accountNumber, request.Amount ?? 0m, cancellationToken);
        return CreatedAtAction(nameof(GetTransactions), new { accountNumber }, transaction);
    }

    /// <summary>Historial cronológico de movimientos con el saldo resultante de cada uno.</summary>
    [HttpGet("{accountNumber}/transactions")]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetTransactions(
        string accountNumber,
        CancellationToken cancellationToken) =>
        Ok(await accountService.GetTransactionsAsync(accountNumber, cancellationToken));
}
