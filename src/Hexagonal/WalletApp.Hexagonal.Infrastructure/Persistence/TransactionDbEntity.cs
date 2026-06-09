using System.ComponentModel.DataAnnotations;

namespace WalletApp.Hexagonal.Infrastructure.Persistence;

/// <summary>ORM entity for transaction persistence; kept separate from the domain Transaction to isolate EF Core concerns.</summary>
public class TransactionDbEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid WalletId { get; set; }

    public Guid? TargetWalletId { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [Required]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; }
}
