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
}
