using Microsoft.EntityFrameworkCore;
using WalletApp.Layered.DataAccess.Entities;

namespace WalletApp.Layered.DataAccess;

/// <summary>EF Core DbContext for the layered wallet application.</summary>
public class WalletDbContext : DbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options) { }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => t.WalletId);
    }
}
