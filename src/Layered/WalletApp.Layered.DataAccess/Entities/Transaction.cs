using System.ComponentModel.DataAnnotations;

namespace WalletApp.Layered.DataAccess.Entities;

/// <summary>EF Core entity representing a wallet transaction in the data access layer.</summary>
public class Transaction
{
    [Key]
    public Guid Id { get; set; }

    public Guid WalletId { get; set; }

    public Guid? TargetWalletId { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty; // Deposit, Withdrawal, Transfer

    public decimal Amount { get; set; }

    [Required]
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed

    public DateTime CreatedAt { get; set; }

    public Wallet Wallet { get; set; } = null!;
}
