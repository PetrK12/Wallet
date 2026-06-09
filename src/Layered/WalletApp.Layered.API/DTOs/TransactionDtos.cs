namespace WalletApp.Layered.API.DTOs;

/// <summary>Request body for deposit or withdrawal operations.</summary>
public record AmountRequest(decimal Amount);

/// <summary>Request body for a transfer between two wallets.</summary>
public record TransferRequest(Guid TargetWalletId, decimal Amount);

/// <summary>Response containing transaction details.</summary>
public record TransactionResponse(
    Guid Id,
    Guid WalletId,
    Guid? TargetWalletId,
    string Type,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt);
