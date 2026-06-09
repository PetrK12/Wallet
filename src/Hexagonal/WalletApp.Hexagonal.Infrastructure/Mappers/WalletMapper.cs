using WalletApp.Hexagonal.Domain.Model;
using WalletApp.Hexagonal.Infrastructure.Persistence;

namespace WalletApp.Hexagonal.Infrastructure.Mappers;

/// <summary>Translates between domain Wallet entities and WalletDbEntity ORM objects.</summary>
public static class WalletMapper
{
    public static Wallet ToDomain(WalletDbEntity e)
        => Wallet.Restore(e.Id, e.OwnerId, e.Balance, e.Currency, e.CreatedAt);

    public static WalletDbEntity ToEntity(Wallet w) => new()
    {
        Id = w.Id,
        OwnerId = w.OwnerId,
        Balance = w.Balance,
        Currency = w.Currency,
        CreatedAt = w.CreatedAt
    };
}
