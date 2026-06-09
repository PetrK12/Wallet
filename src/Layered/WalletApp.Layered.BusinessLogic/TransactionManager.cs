using WalletApp.Layered.DataAccess;
using WalletApp.Layered.DataAccess.Entities;
using WalletApp.Layered.DataAccess.Repositories;

namespace WalletApp.Layered.BusinessLogic;

/// <summary>Contains all business rules for deposits, withdrawals, transfers and transaction history in the layered architecture.</summary>
public class TransactionManager
{
    private const decimal DailyLimit = 10_000m;

    private readonly WalletRepository _walletRepo;
    private readonly TransactionRepository _transactionRepo;
    private readonly WalletDbContext _db;

    public TransactionManager(
        WalletRepository walletRepo,
        TransactionRepository transactionRepo,
        WalletDbContext db)
    {
        _walletRepo = walletRepo;
        _transactionRepo = transactionRepo;
        _db = db;
    }

    /// <summary>Deposits a positive amount into the specified wallet, recording the wallet's currency.</summary>
    public async Task<Transaction> DepositAsync(Guid walletId, decimal amount)
    {
        ValidateAmount(amount);
        var wallet = await GetWalletOrThrowAsync(walletId);

        var tx = CreateTransaction(walletId, null, "Deposit", amount, wallet.Currency, "Pending");
        await _transactionRepo.AddAsync(tx);

        wallet.Balance += amount;
        tx.Status = "Completed";

        await _walletRepo.UpdateAsync(wallet);
        await _transactionRepo.UpdateAsync(tx);
        return tx;
    }

    /// <summary>Withdraws a positive amount from the specified wallet, validating balance and daily limit.</summary>
    public async Task<Transaction> WithdrawAsync(Guid walletId, decimal amount)
    {
        ValidateAmount(amount);
        var wallet = await GetWalletOrThrowAsync(walletId);

        var tx = CreateTransaction(walletId, null, "Withdrawal", amount, wallet.Currency, "Pending");
        await _transactionRepo.AddAsync(tx);

        try
        {
            await ValidateDailyLimitAsync(walletId, amount);
            if (wallet.Balance < amount)
                throw new InvalidOperationException("Insufficient balance.");

            wallet.Balance -= amount;
            tx.Status = "Completed";
            await _walletRepo.UpdateAsync(wallet);
        }
        catch
        {
            tx.Status = "Failed";
        }

        await _transactionRepo.UpdateAsync(tx);
        if (tx.Status == "Failed")
            throw new InvalidOperationException("Transaction failed: insufficient balance or daily limit exceeded.");
        return tx;
    }

    /// <summary>Atomically transfers a positive amount between two wallets. Rejects cross-currency transfers.</summary>
    public async Task<Transaction> TransferAsync(Guid sourceWalletId, Guid targetWalletId, decimal amount)
    {
        ValidateAmount(amount);
        if (sourceWalletId == targetWalletId)
            throw new ArgumentException("Source and target wallets must differ.");

        var source = await GetWalletOrThrowAsync(sourceWalletId);
        var target = await GetWalletOrThrowAsync(targetWalletId);

        if (source.Currency != target.Currency)
            throw new InvalidOperationException(
                $"Currency mismatch: cannot transfer from {source.Currency} wallet to {target.Currency} wallet.");

        var tx = CreateTransaction(sourceWalletId, targetWalletId, "Transfer", amount, source.Currency, "Pending");
        await _transactionRepo.AddAsync(tx);

        await using var dbTx = await _db.Database.BeginTransactionAsync();
        try
        {
            await ValidateDailyLimitAsync(sourceWalletId, amount);
            if (source.Balance < amount)
                throw new InvalidOperationException("Insufficient balance.");

            source.Balance -= amount;
            target.Balance += amount;
            tx.Status = "Completed";

            await _walletRepo.UpdateAsync(source);
            await _walletRepo.UpdateAsync(target);
            await _transactionRepo.UpdateAsync(tx);
            await dbTx.CommitAsync();
        }
        catch
        {
            await dbTx.RollbackAsync();
            tx.Status = "Failed";
            await _transactionRepo.UpdateAsync(tx);
            throw new InvalidOperationException("Transfer failed: insufficient balance, daily limit exceeded, or currency mismatch.");
        }

        return tx;
    }

    /// <summary>Returns the transaction history for a wallet in descending chronological order.</summary>
    public async Task<IReadOnlyList<Transaction>> GetHistoryAsync(Guid walletId)
        => await _transactionRepo.GetByWalletIdAsync(walletId);

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.");
        if (amount > DailyLimit)
            throw new ArgumentException($"Amount exceeds the single-transaction limit of {DailyLimit}.");
    }

    private async Task ValidateDailyLimitAsync(Guid walletId, decimal amount)
    {
        var dailyTotal = await _transactionRepo.GetDailyTotalAsync(walletId, DateTime.UtcNow);
        if (dailyTotal + amount > DailyLimit)
            throw new InvalidOperationException($"Daily limit of {DailyLimit} would be exceeded.");
    }

    private async Task<Wallet> GetWalletOrThrowAsync(Guid walletId)
    {
        var wallet = await _walletRepo.GetByIdAsync(walletId);
        if (wallet is null)
            throw new KeyNotFoundException($"Wallet {walletId} not found.");
        return wallet;
    }

    private static Transaction CreateTransaction(
        Guid walletId, Guid? targetWalletId, string type, decimal amount, string currency, string status)
        => new()
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            TargetWalletId = targetWalletId,
            Type = type,
            Amount = amount,
            Currency = currency,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
}
