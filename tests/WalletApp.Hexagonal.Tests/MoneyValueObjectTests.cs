using WalletApp.Hexagonal.Domain.Model;
using Xunit;

namespace WalletApp.Hexagonal.Tests;

/// <summary>Unit tests for the Money value object's invariants.</summary>
public class MoneyValueObjectTests
{
    [Fact]
    public void Money_PositiveAmount_CreatesSuccessfully()
    {
        var m = new Money(100m);
        Assert.Equal(100m, m.Amount);
    }

    [Fact]
    public void Money_ZeroAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(0m));
    }

    [Fact]
    public void Money_NegativeAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(-50m));
    }

    [Fact]
    public void Money_ExceedsDailyLimit_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(Money.DailyLimit + 0.01m));
    }

    [Fact]
    public void Money_AtDailyLimit_CreatesSuccessfully()
    {
        var m = new Money(Money.DailyLimit);
        Assert.Equal(Money.DailyLimit, m.Amount);
    }

    [Fact]
    public void Money_ComparisonOperatorsWorkCorrectly()
    {
        var small = new Money(50m);
        var large = new Money(100m);

        Assert.True(large > small);
        Assert.True(small < large);
        Assert.True(large >= small);
        Assert.True(small <= large);
        Assert.True(large >= new Money(100m));
        Assert.True(small <= new Money(50m));
        Assert.False(small > large);
        Assert.False(large < small);
    }
}
