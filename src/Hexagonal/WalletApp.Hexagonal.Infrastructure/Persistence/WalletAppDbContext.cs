using Microsoft.EntityFrameworkCore;

namespace WalletApp.Hexagonal.Infrastructure.Persistence;

/// <summary>EF Core DbContext for the hexagonal wallet application. Knows only about ORM entities.</summary>
public class WalletAppDbContext : DbContext
{
    public WalletAppDbContext(DbContextOptions<WalletAppDbContext> options) : base(options) { }

    public DbSet<WalletDbEntity> Wallets => Set<WalletDbEntity>();
    public DbSet<TransactionDbEntity> Transactions => Set<TransactionDbEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) { }
}
