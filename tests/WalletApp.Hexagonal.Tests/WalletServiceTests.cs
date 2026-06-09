using WalletApp.Hexagonal.Domain.Services;
using WalletApp.Hexagonal.Tests.InMemory;
using Xunit;

namespace WalletApp.Hexagonal.Tests;

/// <summary>Unit tests for WalletService using pure in-memory test doubles. No EF or ASP.NET dependency.</summary>
public class WalletServiceTests
{
    private WalletService BuildService() =>
        new(new InMemoryWalletRepository());

    [Fact]
    public async Task CreateWallet_ValidOwner_ReturnsWalletWithZeroBalance()
    {
        var svc = BuildService();

        var wallet = await svc.CreateWalletAsync("user1", "CZK");

        Assert.Equal("user1", wallet.OwnerId);
        Assert.Equal("CZK", wallet.Currency);
        Assert.Equal(0m, wallet.Balance);
    }

    [Fact]
    public async Task CreateWallet_CurrencyIsNormalizedToUppercase()
    {
        var svc = BuildService();

        var wallet = await svc.CreateWalletAsync("user-lower", "czk");

        Assert.Equal("CZK", wallet.Currency);
    }

    [Fact]
    public async Task CreateWallet_DuplicateOwner_Throws()
    {
        var svc = BuildService();
        await svc.CreateWalletAsync("user1", "EUR");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateWalletAsync("user1", "EUR"));
    }

    [Fact]
    public async Task CreateWallet_EmptyOwner_Throws()
    {
        var svc = BuildService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateWalletAsync("", "CZK"));
    }

    [Fact]
    public async Task CreateWallet_EmptyCurrency_Throws()
    {
        var svc = BuildService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateWalletAsync("user2", ""));
    }

    [Fact]
    public async Task GetWallet_ExistingId_ReturnsWallet()
    {
        var svc = BuildService();
        var created = await svc.CreateWalletAsync("user3", "USD");

        var retrieved = await svc.GetWalletAsync(created.Id);

        Assert.Equal(created.Id, retrieved.Id);
    }

    [Fact]
    public async Task GetWallet_UnknownId_Throws()
    {
        var svc = BuildService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.GetWalletAsync(Guid.NewGuid()));
    }
}
