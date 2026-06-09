using WalletApp.Hexagonal.Domain.Model;

namespace WalletApp.Hexagonal.Domain.Ports.Input;

/// <summary>Input port (driving adapter contract) for wallet use cases. Implemented by domain services.</summary>
public interface IWalletUseCase
{
    Task<Wallet> CreateWalletAsync(string ownerId, string currency);
    Task<Wallet> GetWalletAsync(Guid walletId);
}
