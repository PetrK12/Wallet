using Microsoft.AspNetCore.Mvc;
using WalletApp.Hexagonal.API.DTOs;
using WalletApp.Hexagonal.Domain.Model;
using WalletApp.Hexagonal.Domain.Ports.Input;

namespace WalletApp.Hexagonal.API.Controllers;

/// <summary>Driving adapter exposing transaction use cases as REST endpoints. Depends only on input port interfaces.</summary>
[ApiController]
[Route("api/wallets/{walletId:guid}")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionUseCase _transactionUseCase;

    public TransactionController(ITransactionUseCase transactionUseCase)
        => _transactionUseCase = transactionUseCase;

    /// <summary>Deposits funds into a wallet.</summary>
    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit(Guid walletId, [FromBody] AmountRequest request)
        => await ExecuteTransaction(() => _transactionUseCase.DepositAsync(walletId, request.Amount));

    /// <summary>Withdraws funds from a wallet.</summary>
    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw(Guid walletId, [FromBody] AmountRequest request)
        => await ExecuteTransaction(() => _transactionUseCase.WithdrawAsync(walletId, request.Amount));

    /// <summary>Transfers funds from one wallet to another.</summary>
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(Guid walletId, [FromBody] TransferRequest request)
        => await ExecuteTransaction(() => _transactionUseCase.TransferAsync(walletId, request.TargetWalletId, request.Amount));

    /// <summary>Returns the transaction history for a wallet.</summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetHistory(Guid walletId)
    {
        try
        {
            var txs = await _transactionUseCase.GetHistoryAsync(walletId);
            return Ok(txs.Select(Map));
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    private async Task<IActionResult> ExecuteTransaction(Func<Task<Transaction>> action)
    {
        try
        {
            var tx = await action();
            return Ok(Map(tx));
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(ex.Message); }
    }

    private static TransactionResponse Map(Transaction t) =>
        new(t.Id, t.WalletId, t.TargetWalletId, t.Type.ToString(), t.Amount.Amount, t.Status.ToString(), t.CreatedAt);
}
