using Microsoft.AspNetCore.Mvc;
using WalletApp.Layered.API.DTOs;
using WalletApp.Layered.BusinessLogic;
using WalletApp.Layered.DataAccess.Entities;

namespace WalletApp.Layered.API.Controllers;

/// <summary>REST controller exposing deposit, withdrawal, transfer and history endpoints in the layered architecture.</summary>
[ApiController]
[Route("api/wallets/{walletId:guid}")]
public class TransactionController : ControllerBase
{
    private readonly TransactionManager _transactionManager;

    public TransactionController(TransactionManager transactionManager)
        => _transactionManager = transactionManager;

    /// <summary>Deposits funds into a wallet.</summary>
    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit(Guid walletId, [FromBody] AmountRequest request)
        => await ExecuteTransaction(() => _transactionManager.DepositAsync(walletId, request.Amount));

    /// <summary>Withdraws funds from a wallet.</summary>
    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw(Guid walletId, [FromBody] AmountRequest request)
        => await ExecuteTransaction(() => _transactionManager.WithdrawAsync(walletId, request.Amount));

    /// <summary>Transfers funds from one wallet to another.</summary>
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(Guid walletId, [FromBody] TransferRequest request)
        => await ExecuteTransaction(() => _transactionManager.TransferAsync(walletId, request.TargetWalletId, request.Amount));

    /// <summary>Returns the transaction history for a wallet.</summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetHistory(Guid walletId)
    {
        try
        {
            var txs = await _transactionManager.GetHistoryAsync(walletId);
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
        new(t.Id, t.WalletId, t.TargetWalletId, t.Type, t.Amount, t.Currency, t.Status, t.CreatedAt);
}
