namespace WalletApp.Hexagonal.Domain.Model;

/// <summary>Value object representing a monetary amount. Enforces positivity and the daily limit.</summary>
public readonly record struct Money
{
    public const decimal DailyLimit = 10_000m;

    public decimal Amount { get; }

    public Money(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        if (amount > DailyLimit)
            throw new ArgumentException($"Amount exceeds the single-transaction limit of {DailyLimit}.", nameof(amount));
        Amount = amount;
    }

    public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount);
    public static bool operator >(Money a, Money b) => a.Amount > b.Amount;
    public static bool operator <(Money a, Money b) => a.Amount < b.Amount;
    public static bool operator >=(Money a, Money b) => a.Amount >= b.Amount;
    public static bool operator <=(Money a, Money b) => a.Amount <= b.Amount;

    public override string ToString() => Amount.ToString("F2");
}
