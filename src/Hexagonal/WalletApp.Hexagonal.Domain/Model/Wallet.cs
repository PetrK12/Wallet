namespace WalletApp.Hexagonal.Domain.Model;

/// <summary>Pure domain entity representing a user's wallet. Business rules are enforced through domain methods.</summary>
public class Wallet
{
    public Guid Id { get; private set; }
    public string OwnerId { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Wallet() { }

    /// <summary>Restores a wallet from persisted state (used by the infrastructure mapper).</summary>
    public static Wallet Restore(Guid id, string ownerId, decimal balance, DateTime createdAt)
        => new() { Id = id, OwnerId = ownerId, Balance = balance, CreatedAt = createdAt };

    public static Wallet Create(string ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("OwnerId must not be empty.", nameof(ownerId));

        return new Wallet
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Balance = 0m,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Applies a deposit; balance increases by the given amount.</summary>
    public void Deposit(Money amount) => Balance += amount.Amount;

    /// <summary>Applies a withdrawal. Throws <see cref="InvalidOperationException"/> if balance is insufficient.</summary>
    public void Withdraw(Money amount)
    {
        if (Balance < amount.Amount)
            throw new InvalidOperationException("Insufficient balance.");
        Balance -= amount.Amount;
    }

    /// <summary>Receives incoming funds from a transfer.</summary>
    public void Receive(Money amount) => Balance += amount.Amount;
}
