using Microsoft.EntityFrameworkCore;
using WalletApp.Hexagonal.Domain.Model;
using WalletApp.Hexagonal.Domain.Ports.Output;
using WalletApp.Hexagonal.Infrastructure.Mappers;
using WalletApp.Hexagonal.Infrastructure.Persistence;

namespace WalletApp.Hexagonal.Infrastructure.Adapters;

/// <summary>Driven adapter implementing ITransactionRepository using EF Core and SQLite.</summary>
public class EfTransactionRepository : ITransactionRepository
{
    private readonly WalletAppDbContext _db;

    public EfTransactionRepository(WalletAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Transaction>> GetByWalletIdAsync(Guid walletId)
        => (await _db.Transactions
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync())
            .Select(TransactionMapper.ToDomain)
            .ToList();

    public async Task AddAsync(Transaction transaction)
    {
        _db.Transactions.Add(TransactionMapper.ToEntity(transaction));
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        var entity = await _db.Transactions.FindAsync(transaction.Id);
        if (entity is null) return;
        entity.Status = transaction.Status.ToString();
        await _db.SaveChangesAsync();
    }

    public async Task<decimal> GetDailyTotalAsync(Guid walletId, DateTime date)
        => await _db.Transactions
            .Where(t => t.WalletId == walletId
                && t.Status == "Completed"
                && t.CreatedAt.Date == date.Date
                && (t.Type == "Withdrawal" || t.Type == "Transfer"))
            .SumAsync(t => t.Amount);

    public async Task ExecuteInTransactionAsync(Func<Task> work)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            await work();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
