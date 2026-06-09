using Microsoft.EntityFrameworkCore;
using WalletApp.Layered.DataAccess.Entities;

namespace WalletApp.Layered.DataAccess.Repositories;

/// <summary>Concrete repository providing data access for wallets using EF Core.</summary>
public class WalletRepository
{
    private readonly WalletDbContext _db;

    public WalletRepository(WalletDbContext db) => _db = db;

    public async Task<Wallet?> GetByIdAsync(Guid id)
        => await _db.Wallets.FindAsync(id);

    public async Task<Wallet?> GetByOwnerIdAsync(string ownerId)
        => await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerId == ownerId);

    public async Task AddAsync(Wallet wallet)
    {
        _db.Wallets.Add(wallet);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Wallet wallet)
    {
        _db.Wallets.Update(wallet);
        await _db.SaveChangesAsync();
    }
}
