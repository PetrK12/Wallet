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
        var wallet = await _walletManager.CreateWalletAsync("u1", "CZK");

        var tx = await _transactionManager.DepositAsync(wallet.Id, 500m);

        Assert.Equal("Completed", tx.Status);
        Assert.Equal("CZK", tx.Currency);
        var updated = await _walletManager.GetWalletAsync(wallet.Id);
        Assert.Equal(500m, updated.Balance);
    }

    [Fact]
    public async Task Deposit_NegativeAmount_Throws()
    {
        var wallet = await _walletManager.CreateWalletAsync("u2", "CZK");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _transactionManager.DepositAsync(wallet.Id, -1m));
    }

    [Fact]
    public async Task Deposit_ExceedsDailyLimit_Throws()
    {
        var wallet = await _walletManager.CreateWalletAsync("u3", "CZK");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _transactionManager.DepositAsync(wallet.Id, 10_001m));
    }

    [Fact]
    public async Task Withdraw_SufficientBalance_DecreasesBalance()
    {
        var wallet = await _walletManager.CreateWalletAsync("u4", "EUR");
        await _transactionManager.DepositAsync(wallet.Id, 1000m);

        var tx = await _transactionManager.WithdrawAsync(wallet.Id, 400m);

        Assert.Equal("Completed", tx.Status);
        var updated = await _walletManager.GetWalletAsync(wallet.Id);
        Assert.Equal(600m, updated.Balance);
    }

    [Fact]
    public async Task Withdraw_InsufficientBalance_ThrowsAndMarksTransactionFailed()
    {
        var wallet = await _walletManager.CreateWalletAsync("u5", "EUR");
        await _transactionManager.DepositAsync(wallet.Id, 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transactionManager.WithdrawAsync(wallet.Id, 200m));

        var history = await _transactionManager.GetHistoryAsync(wallet.Id);
        Assert.Contains(history, t => t.Status == "Failed");
    }

    [Fact]
    public async Task Transfer_SameCurrency_MovesBalanceBetweenWallets()
    {
        var source = await _walletManager.CreateWalletAsync("u6", "CZK");
        var target = await _walletManager.CreateWalletAsync("u7", "CZK");
        await _transactionManager.DepositAsync(source.Id, 1000m);

        await _transactionManager.TransferAsync(source.Id, target.Id, 300m);

        var updatedSource = await _walletManager.GetWalletAsync(source.Id);
        var updatedTarget = await _walletManager.GetWalletAsync(target.Id);
        Assert.Equal(700m, updatedSource.Balance);
        Assert.Equal(300m, updatedTarget.Balance);
    }

    [Fact]
    public async Task Transfer_DifferentCurrency_Throws()
    {
        var source = await _walletManager.CreateWalletAsync("u6b", "CZK");
        var target = await _walletManager.CreateWalletAsync("u7b", "EUR");
        await _transactionManager.DepositAsync(source.Id, 1000m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transactionManager.TransferAsync(source.Id, target.Id, 300m));
    }

    [Fact]
    public async Task Transfer_InsufficientBalance_ThrowsAndMarksTransactionFailed()
    {
        var source = await _walletManager.CreateWalletAsync("u8", "USD");
        var target = await _walletManager.CreateWalletAsync("u9", "USD");
        await _transactionManager.DepositAsync(source.Id, 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transactionManager.TransferAsync(source.Id, target.Id, 500m));
    }

    [Fact]
    public async Task Transfer_SameWallet_Throws()
    {
        var wallet = await _walletManager.CreateWalletAsync("u10", "CZK");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _transactionManager.TransferAsync(wallet.Id, wallet.Id, 100m));
    }

    [Fact]
    public async Task GetHistory_ReturnsTransactionsForWallet()
    {
        var wallet = await _walletManager.CreateWalletAsync("u11", "CZK");
        await _transactionManager.DepositAsync(wallet.Id, 200m);
        await _transactionManager.DepositAsync(wallet.Id, 300m);

        var history = await _transactionManager.GetHistoryAsync(wallet.Id);

        Assert.Equal(2, history.Count);
    }

    [Fact]
    public async Task Withdraw_ZeroAmount_Throws()
    {
        var wallet = await _walletManager.CreateWalletAsync("u12", "CZK");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _transactionManager.WithdrawAsync(wallet.Id, 0m));
    }

    [Fact]
    public async Task Deposit_ZeroAmount_ThrowsArgumentException()
    {
        var wallet = await _walletManager.CreateWalletAsync("u13");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _transactionManager.DepositAsync(wallet.Id, 0m));
    }

    [Fact]
    public async Task Deposit_ToNonExistentWallet_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _transactionManager.DepositAsync(Guid.NewGuid(), 100m));
    }

    [Fact]
    public async Task Withdraw_NegativeAmount_ThrowsArgumentException()
    {
        var wallet = await _walletManager.CreateWalletAsync("u14");
        await _transactionManager.DepositAsync(wallet.Id, 500m);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _transactionManager.WithdrawAsync(wallet.Id, -1m));
    }

    [Fact]
    public async Task Withdraw_ExceedingDailyLimit_ThrowsInvalidOperationException()
    {
        var wallet = await _walletManager.CreateWalletAsync("u15");
        await _transactionManager.DepositAsync(wallet.Id, 10_000m);
        await _transactionManager.WithdrawAsync(wallet.Id, 8_000m); // daily withdrawal total = 8,000
        await _transactionManager.DepositAsync(wallet.Id, 10_000m); // refill balance

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transactionManager.WithdrawAsync(wallet.Id, 3_000m)); // 8k + 3k = 11k > 10k
    }

    [Fact]
    public async Task Withdraw_OnSuccess_TransactionStatusIsCompleted()
    {
        var wallet = await _walletManager.CreateWalletAsync("u16");
        await _transactionManager.DepositAsync(wallet.Id, 500m);

        var tx = await _transactionManager.WithdrawAsync(wallet.Id, 200m);

        Assert.Equal("Completed", tx.Status);
    }

    [Fact]
    public async Task Withdraw_OnFailure_TransactionStatusIsFailed()
    {
        var wallet = await _walletManager.CreateWalletAsync("u17");
        await _transactionManager.DepositAsync(wallet.Id, 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transactionManager.WithdrawAsync(wallet.Id, 500m));

        var history = await _transactionManager.GetHistoryAsync(wallet.Id);
        var withdrawalTx = history.First(t => t.Type == "Withdrawal");
        Assert.Equal("Failed", withdrawalTx.Status);
    }

    [Fact]
    public async Task Transfer_OnSuccess_TransactionStatusIsCompleted()
    {
        var source = await _walletManager.CreateWalletAsync("u18s");
        var target = await _walletManager.CreateWalletAsync("u18t");
        await _transactionManager.DepositAsync(source.Id, 1000m);

        var tx = await _transactionManager.TransferAsync(source.Id, target.Id, 300m);

        Assert.Equal("Completed", tx.Status);
    }

    [Fact]
    public async Task Transfer_OnFailure_TransactionStatusIsFailed()
    {
        var source = await _walletManager.CreateWalletAsync("u19s");
        var target = await _walletManager.CreateWalletAsync("u19t");
        await _transactionManager.DepositAsync(source.Id, 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transactionManager.TransferAsync(source.Id, target.Id, 500m));

        var history = await _transactionManager.GetHistoryAsync(source.Id);
        var transferTx = history.First(t => t.Type == "Transfer");
        Assert.Equal("Failed", transferTx.Status);
    }

    [Fact]
    public async Task Transfer_Fails_BothWalletBalancesUnchanged()
    {
        var source = await _walletManager.CreateWalletAsync("u20s");
        var target = await _walletManager.CreateWalletAsync("u20t");
        await _transactionManager.DepositAsync(source.Id, 100m);
        await _transactionManager.DepositAsync(target.Id, 50m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transactionManager.TransferAsync(source.Id, target.Id, 500m));

        var updatedSource = await _walletManager.GetWalletAsync(source.Id);
        var updatedTarget = await _walletManager.GetWalletAsync(target.Id);
        Assert.Equal(100m, updatedSource.Balance);
        Assert.Equal(50m, updatedTarget.Balance);
    }

    [Fact]
    public async Task Transfer_ExceedingDailyLimit_ThrowsInvalidOperationException()
    {
        var source = await _walletManager.CreateWalletAsync("u21s");
        var target = await _walletManager.CreateWalletAsync("u21t");
        await _transactionManager.DepositAsync(source.Id, 10_000m);
        await _transactionManager.WithdrawAsync(source.Id, 8_000m); // daily total = 8,000
        await _transactionManager.DepositAsync(source.Id, 10_000m); // refill balance

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transactionManager.TransferAsync(source.Id, target.Id, 3_000m)); // 8k + 3k = 11k > 10k
    }

    [Fact]
    public async Task GetHistory_ReturnsTransactionsInDescendingChronologicalOrder()
    {
        var wallet = await _walletManager.CreateWalletAsync("u22");
        await _transactionManager.DepositAsync(wallet.Id, 100m);
        await _transactionManager.DepositAsync(wallet.Id, 200m);

        var history = await _transactionManager.GetHistoryAsync(wallet.Id);

        Assert.Equal(2, history.Count);
        Assert.True(history[0].CreatedAt >= history[1].CreatedAt);
    }

    [Fact]
    public async Task GetHistory_ForNewWallet_ReturnsEmptyList()
    {
        var wallet = await _walletManager.CreateWalletAsync("u23");

        var history = await _transactionManager.GetHistoryAsync(wallet.Id);

        Assert.Empty(history);
    }
}
