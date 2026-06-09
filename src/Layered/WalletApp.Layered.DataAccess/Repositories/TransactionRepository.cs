using Microsoft.EntityFrameworkCore;
using WalletApp.Layered.DataAccess.Entities;

namespace WalletApp.Layered.DataAccess.Repositories;

/// <summary>Concrete repository providing data access for transactions using EF Core.</summary>
public class TransactionRepository
{
    private readonly WalletDbContext _db;

    public TransactionRepository(WalletDbContext db) => _db = db;

    public async Task<Transaction?> GetByIdAsync(Guid id)
        => await _db.Transactions.FindAsync(id);

    public async Task<IReadOnlyList<Transaction>> GetByWalletIdAsync(Guid walletId)
        => await _db.Transactions
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(Transaction transaction)
    {
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        _db.Transactions.Update(transaction);
        await _db.SaveChangesAsync();
    }

    public async Task<decimal> GetDailyTotalAsync(Guid walletId, DateTime date)
        => await _db.Transactions
            .Where(t => t.WalletId == walletId
                && t.Status == "Completed"
                && t.CreatedAt.Date == date.Date
                && (t.Type == "Withdrawal" || t.Type == "Transfer"))
            .SumAsync(t => t.Amount);
}
