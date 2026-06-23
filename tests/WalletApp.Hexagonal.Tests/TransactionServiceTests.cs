using WalletApp.Hexagonal.Domain.Model;
using WalletApp.Hexagonal.Domain.Services;
using WalletApp.Hexagonal.Tests.InMemory;
using Xunit;

namespace WalletApp.Hexagonal.Tests;

/// <summary>Unit tests for TransactionService and domain rules using pure in-memory test doubles. No EF or ASP.NET dependency.</summary>
public class TransactionServiceTests
{
    private (WalletService walletSvc, TransactionService txSvc) BuildServices()
    {
        var walletRepo = new InMemoryWalletRepository();
        var txRepo = new InMemoryTransactionRepository();
        return (new WalletService(walletRepo), new TransactionService(walletRepo, txRepo));
    }

    [Fact]
    public async Task Deposit_ValidAmount_IncreasesBalance()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u1");

        var tx = await txSvc.DepositAsync(wallet.Id, 500m);

        Assert.Equal(500m, wallet.Balance);
    }

    [Fact]
    public async Task Deposit_NegativeAmount_Throws()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u2");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            txSvc.DepositAsync(wallet.Id, -10m));
    }

    [Fact]
    public async Task Deposit_ExceedsSingleTransactionLimit_Throws()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u3");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            txSvc.DepositAsync(wallet.Id, 10_001m));
    }

    [Fact]
    public async Task Withdraw_SufficientBalance_DecreasesBalance()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u4");
        await txSvc.DepositAsync(wallet.Id, 1000m);

        await txSvc.WithdrawAsync(wallet.Id, 400m);

        Assert.Equal(600m, wallet.Balance);
    }

    [Fact]
    public async Task Withdraw_InsufficientBalance_ThrowsAndTransactionFailed()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u5");
        await txSvc.DepositAsync(wallet.Id, 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            txSvc.WithdrawAsync(wallet.Id, 200m));
    }

    [Fact]
    public async Task Transfer_ValidTransfer_MovesBalanceBetweenWallets()
    {
        var (walletSvc, txSvc) = BuildServices();
        var source = await walletSvc.CreateWalletAsync("u6");
        var target = await walletSvc.CreateWalletAsync("u7");
        await txSvc.DepositAsync(source.Id, 1000m);

        await txSvc.TransferAsync(source.Id, target.Id, 300m);

        Assert.Equal(700m, source.Balance);
        Assert.Equal(300m, target.Balance);
    }

    [Fact]
    public async Task Transfer_InsufficientBalance_Throws()
    {
        var (walletSvc, txSvc) = BuildServices();
        var source = await walletSvc.CreateWalletAsync("u8");
        var target = await walletSvc.CreateWalletAsync("u9");
        await txSvc.DepositAsync(source.Id, 50m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            txSvc.TransferAsync(source.Id, target.Id, 200m));
    }

    [Fact]
    public async Task Transfer_SameWallet_Throws()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u10");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            txSvc.TransferAsync(wallet.Id, wallet.Id, 100m));
    }

    [Fact]
    public async Task GetHistory_ReturnsTransactionsForWallet()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u11");
        await txSvc.DepositAsync(wallet.Id, 100m);
        await txSvc.DepositAsync(wallet.Id, 200m);

        var history = await txSvc.GetHistoryAsync(wallet.Id);

        Assert.Equal(2, history.Count);
    }

    [Fact]
    public async Task Withdraw_ZeroAmount_Throws()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u12");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            txSvc.WithdrawAsync(wallet.Id, 0m));
    }

    [Fact]
    public async Task Deposit_ZeroAmount_ThrowsArgumentException()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u13");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            txSvc.DepositAsync(wallet.Id, 0m));
    }

    [Fact]
    public async Task Deposit_ToNonExistentWallet_ThrowsKeyNotFoundException()
    {
        var (_, txSvc) = BuildServices();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            txSvc.DepositAsync(Guid.NewGuid(), 100m));
    }

    [Fact]
    public async Task Withdraw_NegativeAmount_ThrowsArgumentException()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u14");
        await txSvc.DepositAsync(wallet.Id, 500m);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            txSvc.WithdrawAsync(wallet.Id, -1m));
    }

    [Fact]
    public async Task Withdraw_ExceedingDailyLimit_ThrowsInvalidOperationException()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u15");
        await txSvc.DepositAsync(wallet.Id, 10_000m);
        await txSvc.WithdrawAsync(wallet.Id, 8_000m); // daily withdrawal total = 8,000
        await txSvc.DepositAsync(wallet.Id, 10_000m); // refill balance

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            txSvc.WithdrawAsync(wallet.Id, 3_000m)); // 8k + 3k = 11k > 10k
    }

    [Fact]
    public async Task Withdraw_OnSuccess_TransactionStatusIsCompleted()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u16");
        await txSvc.DepositAsync(wallet.Id, 500m);

        var tx = await txSvc.WithdrawAsync(wallet.Id, 200m);

        Assert.Equal(TransactionStatus.Completed, tx.Status);
    }

    [Fact]
    public async Task Withdraw_OnFailure_TransactionStatusIsFailed()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u17");
        await txSvc.DepositAsync(wallet.Id, 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            txSvc.WithdrawAsync(wallet.Id, 500m));

        var history = await txSvc.GetHistoryAsync(wallet.Id);
        var withdrawalTx = history.First(t => t.Type == TransactionType.Withdrawal);
        Assert.Equal(TransactionStatus.Failed, withdrawalTx.Status);
    }

    [Fact]
    public async Task Transfer_OnSuccess_TransactionStatusIsCompleted()
    {
        var (walletSvc, txSvc) = BuildServices();
        var source = await walletSvc.CreateWalletAsync("u18s");
        var target = await walletSvc.CreateWalletAsync("u18t");
        await txSvc.DepositAsync(source.Id, 1000m);

        var tx = await txSvc.TransferAsync(source.Id, target.Id, 300m);

        Assert.Equal(TransactionStatus.Completed, tx.Status);
    }

    [Fact]
    public async Task Transfer_OnFailure_TransactionStatusIsFailed()
    {
        var (walletSvc, txSvc) = BuildServices();
        var source = await walletSvc.CreateWalletAsync("u19s");
        var target = await walletSvc.CreateWalletAsync("u19t");
        await txSvc.DepositAsync(source.Id, 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            txSvc.TransferAsync(source.Id, target.Id, 500m));

        var history = await txSvc.GetHistoryAsync(source.Id);
        var transferTx = history.First(t => t.Type == TransactionType.Transfer);
        Assert.Equal(TransactionStatus.Failed, transferTx.Status);
    }

    [Fact]
    public async Task Transfer_Fails_BothWalletBalancesUnchanged()
    {
        var (walletSvc, txSvc) = BuildServices();
        var source = await walletSvc.CreateWalletAsync("u20s");
        var target = await walletSvc.CreateWalletAsync("u20t");
        await txSvc.DepositAsync(source.Id, 100m);
        await txSvc.DepositAsync(target.Id, 50m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            txSvc.TransferAsync(source.Id, target.Id, 500m));

        Assert.Equal(100m, source.Balance);
        Assert.Equal(50m, target.Balance);
    }

    [Fact]
    public async Task Transfer_ExceedingDailyLimit_ThrowsInvalidOperationException()
    {
        var (walletSvc, txSvc) = BuildServices();
        var source = await walletSvc.CreateWalletAsync("u21s");
        var target = await walletSvc.CreateWalletAsync("u21t");
        await txSvc.DepositAsync(source.Id, 10_000m);
        await txSvc.WithdrawAsync(source.Id, 8_000m); // daily total = 8,000
        await txSvc.DepositAsync(source.Id, 10_000m); // refill balance

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            txSvc.TransferAsync(source.Id, target.Id, 3_000m)); // 8k + 3k = 11k > 10k
    }

    [Fact]
    public async Task GetHistory_ReturnsTransactionsInDescendingChronologicalOrder()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u22");
        await txSvc.DepositAsync(wallet.Id, 100m);
        await txSvc.DepositAsync(wallet.Id, 200m);

        var history = await txSvc.GetHistoryAsync(wallet.Id);

        Assert.Equal(2, history.Count);
        Assert.True(history[0].CreatedAt >= history[1].CreatedAt);
    }

    [Fact]
    public async Task GetHistory_ForNewWallet_ReturnsEmptyList()
    {
        var (walletSvc, txSvc) = BuildServices();
        var wallet = await walletSvc.CreateWalletAsync("u23");

        var history = await txSvc.GetHistoryAsync(wallet.Id);

        Assert.Empty(history);
    }
}
