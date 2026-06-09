using WalletApp.Hexagonal.Domain.Model;
using WalletApp.Hexagonal.Domain.Ports.Input;
using WalletApp.Hexagonal.Domain.Ports.Output;

namespace WalletApp.Hexagonal.Domain.Services;

/// <summary>Application service implementing the transaction input port. Depends only on output port abstractions.</summary>
public class TransactionService : ITransactionUseCase
{
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;

    public TransactionService(IWalletRepository walletRepo, ITransactionRepository transactionRepo)
    {
        _walletRepo = walletRepo;
        _transactionRepo = transactionRepo;
    }

    public async Task<Transaction> DepositAsync(Guid walletId, decimal amount)
    {
        var money = new Money(amount);
        var wallet = await GetWalletOrThrowAsync(walletId);

        var tx = Transaction.Create(walletId, null, TransactionType.Deposit, money, wallet.Currency);
        await _transactionRepo.AddAsync(tx);

        wallet.Deposit(money);
        tx.Complete();

        await _walletRepo.UpdateAsync(wallet);
        await _transactionRepo.UpdateAsync(tx);
        return tx;
    }

    public async Task<Transaction> WithdrawAsync(Guid walletId, decimal amount)
    {
        var money = new Money(amount);
        var wallet = await GetWalletOrThrowAsync(walletId);

        var tx = Transaction.Create(walletId, null, TransactionType.Withdrawal, money, wallet.Currency);
        await _transactionRepo.AddAsync(tx);

        try
        {
            await CheckDailyLimitAsync(walletId, money);
            wallet.Withdraw(money);
            tx.Complete();
            await _walletRepo.UpdateAsync(wallet);
        }
        catch
        {
            tx.Fail();
            await _transactionRepo.UpdateAsync(tx);
            throw new InvalidOperationException("Withdrawal failed: insufficient balance or daily limit exceeded.");
        }

        await _transactionRepo.UpdateAsync(tx);
        return tx;
    }

    public async Task<Transaction> TransferAsync(Guid sourceWalletId, Guid targetWalletId, decimal amount)
    {
        if (sourceWalletId == targetWalletId)
            throw new ArgumentException("Source and target wallets must differ.");

        var money = new Money(amount);
        var source = await GetWalletOrThrowAsync(sourceWalletId);
        var target = await GetWalletOrThrowAsync(targetWalletId);

        // Currency rule enforced on the domain entity itself
        source.EnsureSameCurrency(target);

        var tx = Transaction.Create(sourceWalletId, targetWalletId, TransactionType.Transfer, money, source.Currency);
        await _transactionRepo.AddAsync(tx);

        try
        {
            await _transactionRepo.ExecuteInTransactionAsync(async () =>
            {
                await CheckDailyLimitAsync(sourceWalletId, money);
                source.Withdraw(money);
                target.Receive(money);
                tx.Complete();

                await _walletRepo.UpdateAsync(source);
                await _walletRepo.UpdateAsync(target);
                await _transactionRepo.UpdateAsync(tx);
            });
        }
        catch
        {
            tx.Fail();
            await _transactionRepo.UpdateAsync(tx);
            throw new InvalidOperationException("Transfer failed: insufficient balance or daily limit exceeded.");
        }

        return tx;
    }

    public async Task<IReadOnlyList<Transaction>> GetHistoryAsync(Guid walletId)
        => await _transactionRepo.GetByWalletIdAsync(walletId);

    private async Task<Wallet> GetWalletOrThrowAsync(Guid walletId)
    {
        var wallet = await _walletRepo.GetByIdAsync(walletId);
        if (wallet is null) throw new KeyNotFoundException($"Wallet {walletId} not found.");
        return wallet;
    }

    private async Task CheckDailyLimitAsync(Guid walletId, Money amount)
    {
        var daily = await _transactionRepo.GetDailyTotalAsync(walletId, DateTime.UtcNow);
        if (daily + amount.Amount > Money.DailyLimit)
            throw new InvalidOperationException($"Daily limit of {Money.DailyLimit} would be exceeded.");
    }
}
