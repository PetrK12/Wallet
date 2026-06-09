using Microsoft.AspNetCore.Mvc;
using WalletApp.Hexagonal.API.DTOs;
using WalletApp.Hexagonal.Domain.Ports.Input;

namespace WalletApp.Hexagonal.API.Controllers;

/// <summary>Driving adapter exposing wallet use cases as REST endpoints. Depends only on input port interfaces.</summary>
[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly IWalletUseCase _walletUseCase;

    public WalletController(IWalletUseCase walletUseCase) => _walletUseCase = walletUseCase;

    /// <summary>Creates a wallet for the specified owner with the given currency.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateWallet([FromBody] CreateWalletRequest request)
    {
        try
        {
            var wallet = await _walletUseCase.CreateWalletAsync(request.OwnerId, request.Currency);
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
            var wallet = await _walletUseCase.GetWalletAsync(id);
            return Ok(new WalletResponse(wallet.Id, wallet.OwnerId, wallet.Balance, wallet.Currency, wallet.CreatedAt));
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }
}
