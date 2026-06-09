using WalletApp.Hexagonal.Domain.Model;
using WalletApp.Hexagonal.Domain.Ports.Input;
using WalletApp.Hexagonal.Domain.Ports.Output;

namespace WalletApp.Hexagonal.Domain.Services;

/// <summary>Application service implementing the wallet input port. Depends only on output port abstractions.</summary>
public class WalletService : IWalletUseCase
{
    private readonly IWalletRepository _walletRepo;

    public WalletService(IWalletRepository walletRepo) => _walletRepo = walletRepo;

    public async Task<Wallet> CreateWalletAsync(string ownerId)
    {
        var existing = await _walletRepo.GetByOwnerIdAsync(ownerId);
        if (existing is not null)
            throw new InvalidOperationException($"Wallet already exists for owner '{ownerId}'.");

        var wallet = Wallet.Create(ownerId);
        await _walletRepo.AddAsync(wallet);
        return wallet;
    }

    public async Task<Wallet> GetWalletAsync(Guid walletId)
    {
        var wallet = await _walletRepo.GetByIdAsync(walletId);
        if (wallet is null)
            throw new KeyNotFoundException($"Wallet {walletId} not found.");
        return wallet;
    }
}
