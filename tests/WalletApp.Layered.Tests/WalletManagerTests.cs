using Microsoft.EntityFrameworkCore;
using WalletApp.Layered.BusinessLogic;
using WalletApp.Layered.DataAccess;
using WalletApp.Layered.DataAccess.Repositories;
using Xunit;

namespace WalletApp.Layered.Tests;

/// <summary>Unit tests for WalletManager using an in-memory database.</summary>
public class WalletManagerTests : IDisposable
{
    private readonly WalletDbContext _db;
    private readonly WalletManager _manager;

    public WalletManagerTests()
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new WalletDbContext(options);
        _manager = new WalletManager(new WalletRepository(_db));
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateWallet_ValidOwner_ReturnsWalletWithZeroBalance()
    {
        var wallet = await _manager.CreateWalletAsync("user1", "CZK");

        Assert.Equal("user1", wallet.OwnerId);
        Assert.Equal("CZK", wallet.Currency);
        Assert.Equal(0m, wallet.Balance);
    }

    [Fact]
    public async Task CreateWallet_CurrencyIsNormalizedToUppercase()
    {
        var wallet = await _manager.CreateWalletAsync("user-lower", "czk");

        Assert.Equal("CZK", wallet.Currency);
    }

    [Fact]
    public async Task CreateWallet_DuplicateOwner_Throws()
    {
        await _manager.CreateWalletAsync("user1", "EUR");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _manager.CreateWalletAsync("user1", "EUR"));
    }

    [Fact]
    public async Task CreateWallet_EmptyOwner_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.CreateWalletAsync("", "CZK"));
    }

    [Fact]
    public async Task CreateWallet_EmptyCurrency_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.CreateWalletAsync("user2", ""));
    }

    [Fact]
    public async Task GetWallet_ExistingId_ReturnsWallet()
    {
        var created = await _manager.CreateWalletAsync("user3", "USD");

        var retrieved = await _manager.GetWalletAsync(created.Id);

        Assert.Equal(created.Id, retrieved.Id);
    }

    [Fact]
    public async Task GetWallet_UnknownId_Throws()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.GetWalletAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateWallet_WhitespaceOwnerId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.CreateWalletAsync("   ", "CZK"));
    }

    [Fact]
    public async Task CreateWallet_ValidInputs_ReturnsWalletWithUppercasedCurrencyAndZeroBalance()
    {
        var wallet = await _manager.CreateWalletAsync("alice", "eur");

        Assert.Equal("alice", wallet.OwnerId);
        Assert.Equal("EUR", wallet.Currency);
        Assert.Equal(0m, wallet.Balance);
    }
}
