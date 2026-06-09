using Microsoft.AspNetCore.Mvc;
using WalletApp.Layered.API.DTOs;
using WalletApp.Layered.BusinessLogic;

namespace WalletApp.Layered.API.Controllers;

/// <summary>REST controller exposing wallet management endpoints in the layered architecture.</summary>
[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly WalletManager _walletManager;

    public WalletController(WalletManager walletManager) => _walletManager = walletManager;

    /// <summary>Creates a wallet for the specified owner with the given currency.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest request)
    {
        try
        {
            var wallet = await _walletManager.CreateWalletAsync(request.OwnerId, request.Currency);
            var response = new WalletResponse(wallet.Id, wallet.OwnerId, wallet.Balance, wallet.Currency, wallet.CreatedAt);
            return CreatedAtAction(nameof(GetWallet), new { id = wallet.Id }, response);
        }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>Gets wallet details by wallet ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetWallet(Guid id)
    {
        try
        {
            var wallet = await _walletManager.GetWalletAsync(id);
            return Ok(new WalletResponse(wallet.Id, wallet.OwnerId, wallet.Balance, wallet.Currency, wallet.CreatedAt));
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }
}
