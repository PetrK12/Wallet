using WalletApp.Hexagonal.Domain.Model;
using WalletApp.Hexagonal.Infrastructure.Persistence;

namespace WalletApp.Hexagonal.Infrastructure.Mappers;

/// <summary>Translates between domain Transaction entities and TransactionDbEntity ORM objects.</summary>
public static class TransactionMapper
{
    public static Transaction ToDomain(TransactionDbEntity e)
    {
        var type = Enum.Parse<TransactionType>(e.Type);
        var status = Enum.Parse<TransactionStatus>(e.Status);
        var money = new Money(e.Amount);
        return Transaction.Restore(e.Id, e.WalletId, e.TargetWalletId, type, money, status, e.CreatedAt);
    }

    public static TransactionDbEntity ToEntity(Transaction t) => new()
    {
        Id = t.Id,
        WalletId = t.WalletId,
        TargetWalletId = t.TargetWalletId,
        Type = t.Type.ToString(),
        Amount = t.Amount.Amount,
        Status = t.Status.ToString(),
        CreatedAt = t.CreatedAt
    };
}
