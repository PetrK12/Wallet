using WalletApp.Layered.DataAccess.Entities;
using WalletApp.Layered.DataAccess.Repositories;

namespace WalletApp.Layered.BusinessLogic;

/// <summary>Contains all business logic for wallet creation and retrieval in the layered architecture.</summary>
public class WalletManager
{
    private readonly WalletRepository _walletRepo;

    public WalletManager(WalletRepository walletRepo) => _walletRepo = walletRepo;

    /// <summary>Creates a new wallet for a user with the specified currency. Throws if the user already has a wallet.</summary>
    public async Task<Wallet> CreateWalletAsync(string ownerId, string currency)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("OwnerId must not be empty.", nameof(ownerId));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency must not be empty.", nameof(currency));

        var existing = await _walletRepo.GetByOwnerIdAsync(ownerId);
        if (existing is not null)
            throw new InvalidOperationException($"Wallet already exists for owner '{ownerId}'.");

        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Currency = currency.ToUpperInvariant(),
            Balance = 0m,
            CreatedAt = DateTime.UtcNow
        };

        await _walletRepo.AddAsync(wallet);
        return wallet;
    }

    /// <summary>Retrieves a wallet by its identifier. Throws if not found.</summary>
    public async Task<Wallet> GetWalletAsync(Guid walletId)
    {
        var wallet = await _walletRepo.GetByIdAsync(walletId);
        if (wallet is null)
            throw new KeyNotFoundException($"Wallet {walletId} not found.");
        return wallet;
    }
}
