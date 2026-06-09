using Microsoft.EntityFrameworkCore;
using WalletApp.Hexagonal.Domain.Model;
using WalletApp.Hexagonal.Domain.Ports.Output;
using WalletApp.Hexagonal.Infrastructure.Mappers;
using WalletApp.Hexagonal.Infrastructure.Persistence;

namespace WalletApp.Hexagonal.Infrastructure.Adapters;

/// <summary>Driven adapter implementing IWalletRepository using EF Core and SQLite.</summary>
public class EfWalletRepository : IWalletRepository
{
    private readonly WalletAppDbContext _db;

    public EfWalletRepository(WalletAppDbContext db) => _db = db;

    public async Task<Wallet?> GetByIdAsync(Guid id)
    {
        var e = await _db.Wallets.FindAsync(id);
        return e is null ? null : WalletMapper.ToDomain(e);
    }

    public async Task<Wallet?> GetByOwnerIdAsync(string ownerId)
    {
        var e = await _db.Wallets.FirstOrDefaultAsync(w => w.OwnerId == ownerId);
        return e is null ? null : WalletMapper.ToDomain(e);
    }

    public async Task AddAsync(Wallet wallet)
    {
        _db.Wallets.Add(WalletMapper.ToEntity(wallet));
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Wallet wallet)
    {
        var entity = await _db.Wallets.FindAsync(wallet.Id);
        if (entity is null) return;
        entity.Balance = wallet.Balance;
        await _db.SaveChangesAsync();
    }
}
