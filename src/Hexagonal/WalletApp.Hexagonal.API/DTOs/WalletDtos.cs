namespace WalletApp.Hexagonal.API.DTOs;

/// <summary>Request body for creating a new wallet.</summary>
public record CreateWalletRequest(string OwnerId, string Currency);

/// <summary>Response containing wallet details.</summary>
public record WalletResponse(Guid Id, string OwnerId, decimal Balance, string Currency, DateTime CreatedAt);
