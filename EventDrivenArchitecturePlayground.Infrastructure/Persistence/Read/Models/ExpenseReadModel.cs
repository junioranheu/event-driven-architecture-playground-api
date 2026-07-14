using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventDrivenArchitecturePlayground.Infrastructure.Persistence.Read.Models;

/// <summary>
/// Representa os dados de uma despesa preparados exclusivamente
/// para consultas do lado de leitura do CQRS.
/// </summary>
[Table("expense_read_models")]
public sealed class ExpenseReadModel
{
    private const int MaxItemLength = 150;

    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(MaxItemLength)]
    [Column("item")]
    public string Item { get; set; } = string.Empty;

    [Required]
    [Column("amount", TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [Column("occurred_at")]
    public DateTime OccurredAt { get; set; }
}