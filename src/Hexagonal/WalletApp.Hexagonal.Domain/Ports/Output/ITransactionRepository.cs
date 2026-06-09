using WalletApp.Hexagonal.Domain.Model;

namespace WalletApp.Hexagonal.Domain.Ports.Output;

/// <summary>Output port (driven adapter contract) for transaction persistence. Implemented by infrastructure.</summary>
public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetByWalletIdAsync(Guid walletId);
    Task AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task<decimal> GetDailyTotalAsync(Guid walletId, DateTime date);
    Task ExecuteInTransactionAsync(Func<Task> work);
}
