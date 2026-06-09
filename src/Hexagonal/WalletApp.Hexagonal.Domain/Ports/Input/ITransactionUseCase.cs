using WalletApp.Hexagonal.Domain.Model;

namespace WalletApp.Hexagonal.Domain.Ports.Input;

/// <summary>Input port (driving adapter contract) for transaction use cases. Implemented by domain services.</summary>
public interface ITransactionUseCase
{
    Task<Transaction> DepositAsync(Guid walletId, decimal amount);
    Task<Transaction> WithdrawAsync(Guid walletId, decimal amount);
    Task<Transaction> TransferAsync(Guid sourceWalletId, Guid targetWalletId, decimal amount);
    Task<IReadOnlyList<Transaction>> GetHistoryAsync(Guid walletId);
}
