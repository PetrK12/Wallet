namespace WalletApp.Hexagonal.Domain.Model;

/// <summary>Pure domain entity representing a financial transaction. Encapsulates its own state transitions.</summary>
public class Transaction
{
    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public Guid? TargetWalletId { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Transaction() { }

    /// <summary>Restores a transaction from persisted state (used by the infrastructure mapper).</summary>
    public static Transaction Restore(
        Guid id, Guid walletId, Guid? targetWalletId,
        TransactionType type, Money amount, TransactionStatus status, DateTime createdAt)
        => new()
        {
            Id = id, WalletId = walletId, TargetWalletId = targetWalletId,
            Type = type, Amount = amount, Status = status, CreatedAt = createdAt
        };

    public static Transaction Create(
        Guid walletId,
        Guid? targetWalletId,
        TransactionType type,
        Money amount)
        => new()
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            TargetWalletId = targetWalletId,
            Type = type,
            Amount = amount,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>Marks the transaction as successfully completed.</summary>
    public void Complete() => Status = TransactionStatus.Completed;

    /// <summary>Marks the transaction as failed.</summary>
    public void Fail() => Status = TransactionStatus.Failed;
}
