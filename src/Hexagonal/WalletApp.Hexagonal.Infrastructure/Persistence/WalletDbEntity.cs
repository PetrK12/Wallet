using System.ComponentModel.DataAnnotations;

namespace WalletApp.Hexagonal.Infrastructure.Persistence;

/// <summary>ORM entity for wallet persistence; kept separate from the domain Wallet to isolate EF Core concerns.</summary>
public class WalletDbEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string OwnerId { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; }
}
