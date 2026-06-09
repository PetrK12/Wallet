using System.ComponentModel.DataAnnotations;

namespace WalletApp.Layered.DataAccess.Entities;

/// <summary>EF Core entity representing a user's wallet in the data access layer.</summary>
public class Wallet
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string OwnerId { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    [Required]
    public string Currency { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
