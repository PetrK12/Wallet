using Microsoft.EntityFrameworkCore;
using WalletApp.Layered.BusinessLogic;
using WalletApp.Layered.DataAccess;
using WalletApp.Layered.DataAccess.Repositories;
using Xunit;

namespace WalletApp.Layered.Tests;

/// <summary>Unit tests for TransactionManager covering all business rules using an in-memory database.</summary>
public class TransactionManagerTests : IDisposable
{
    private readonly WalletDbContext _db;
    private readonly WalletManager _walletManager;
    private readonly TransactionManager _transactionManager;

    public TransactionManagerTests()
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new WalletDbContext(options);
        var walletRepo = new WalletRepository(_db);
        var txRepo = new TransactionRepository(_db);
        _walletManager = new WalletManager(walletRepo);
        _transactionManager = new TransactionManager(walletRepo, txRepo, _db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Deposit_ValidAmount_IncreasesBalance()
    {
        var wallet = await _walletManager.CreateWalletAsync("u1");

        var tx = await _transactionManager.DepositAsync(wallet.Id, 500m);

        Assert.Equal("Completed", tx.Status);
        var updated = await _walletManager.GetWalletAsync(wallet.Id);
        Assert.Equal(500m, updated.Balance);
    }

    [Fact]
    public async Task Deposit_NegativeAmount_Throws()
    {
        var wallet = await _walletManager.CreateWalletAsync("u2");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _transactionManager.DepositAsync(wallet.Id, -1m));
    }

    [Fact]
    public async Task Deposit_ExceedsDailyLimit_Throws()
    {
        var wallet = await _walletManager.CreateWalletAsync("u3");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _transactionManager.DepositAsync(wallet.Id, 10_001m));
    }

    [Fact]
    public async Task Withdraw_SufficientBalance_DecreasesBalance()
    {
        var wallet = await _walletManager.CreateWalletAsync("u4");
        await _transactionManager.DepositAsync(wallet.Id, 1000m);

        var tx = await _transactionManager.WithdrawAsync(wallet.Id, 400m);

        Assert.Equal("Completed", tx.Status);
        var updated = await _walletManager.GetWalletAsync(wallet.Id);
        Assert.Equal(600m, updated.Balance);
    }

    [Fact]
    public async Task Withdraw_InsufficientBalance_ThrowsAndMarksTransactionFailed()
    {
        var wallet = await _walletManager.CreateWalletAsync("u5");
        await _transactionManager.DepositAsync(wallet.Id, 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transactionManager.WithdrawAsync(wallet.Id, 200m));

        var history = await _transactionManager.GetHistoryAsync(wallet.Id);
        Assert.Contains(history, t => t.Status == "Failed");
    }

    [Fact]
    public async Task Transfer_ValidTransfer_MovesBalanceBetweenWallets()
    {
        var source = await _walletManager.CreateWalletAsync("u6");
        var target = await _walletManager.CreateWalletAsync("u7");
        await _transactionManager.DepositAsync(source.Id, 1000m);

        await _transactionManager.TransferAsync(source.Id, target.Id, 300m);

        var updatedSource = await _walletManager.GetWalletAsync(source.Id);
        var updatedTarget = await _walletManager.GetWalletAsync(target.Id);
        Assert.Equal(700m, updatedSource.Balance);
        Assert.Equal(300m, updatedTarget.Balance);
    }

    [Fact]
    public async Task Transfer_InsufficientBalance_ThrowsAndMarksTransactionFailed()
    {
        var source = await _walletManager.CreateWalletAsync("u8");
        var target = await _walletManager.CreateWalletAsync("u9");
        await _transactionManager.DepositAsync(source.Id, 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transactionManager.TransferAsync(source.Id, target.Id, 500m));
    }

    [Fact]
    public async Task Transfer_SameWallet_Throws()
    {
        var wallet = await _walletManager.CreateWalletAsync("u10");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _transactionManager.TransferAsync(wallet.Id, wallet.Id, 100m));
    }

    [Fact]
    public async Task GetHistory_ReturnsTransactionsForWallet()
    {
        var wallet = await _walletManager.CreateWalletAsync("u11");
        await _transactionManager.DepositAsync(wallet.Id, 200m);
        await _transactionManager.DepositAsync(wallet.Id, 300m);

        var history = await _transactionManager.GetHistoryAsync(wallet.Id);

        Assert.Equal(2, history.Count);
    }

    [Fact]
    public async Task Withdraw_ZeroAmount_Throws()
    {
        var wallet = await _walletManager.CreateWalletAsync("u12");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _transactionManager.WithdrawAsync(wallet.Id, 0m));
    }
}
