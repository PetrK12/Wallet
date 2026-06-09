using WalletApp.Hexagonal.Domain.Model;
using WalletApp.Hexagonal.Domain.Ports.Output;

namespace WalletApp.Hexagonal.Tests.InMemory;

/// <summary>Simple in-memory test double for ITransactionRepository; no mocking framework required.</summary>
internal class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly List<Transaction> _store = new();

    public Task<IReadOnlyList<Transaction>> GetByWalletIdAsync(Guid walletId)
    {
        IReadOnlyList<Transaction> result = _store
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(Transaction transaction)
    {
        _store.Add(transaction);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Transaction transaction)
        => Task.CompletedTask; // mutations are in-place on reference type

    public Task<decimal> GetDailyTotalAsync(Guid walletId, DateTime date)
    {
        var total = _store
            .Where(t => t.WalletId == walletId
                && t.Status == TransactionStatus.Completed
                && t.CreatedAt.Date == date.Date
                && (t.Type == TransactionType.Withdrawal || t.Type == TransactionType.Transfer))
            .Sum(t => t.Amount.Amount);
        return Task.FromResult(total);
    }

    public async Task ExecuteInTransactionAsync(Func<Task> work)
        => await work(); // no database — just execute directly
}
