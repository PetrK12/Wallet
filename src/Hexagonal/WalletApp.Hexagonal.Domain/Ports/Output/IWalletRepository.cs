using WalletApp.Hexagonal.Domain.Model;

namespace WalletApp.Hexagonal.Domain.Ports.Output;

/// <summary>Output port (driven adapter contract) for wallet persistence. Implemented by infrastructure.</summary>
public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(Guid id);
    Task<Wallet?> GetByOwnerIdAsync(string ownerId);
    Task AddAsync(Wallet wallet);
    Task UpdateAsync(Wallet wallet);
}
