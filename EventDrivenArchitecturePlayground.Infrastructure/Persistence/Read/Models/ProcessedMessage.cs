using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventDrivenArchitecturePlayground.Infrastructure.Persistence.Read.Models;

/// <summary>
/// Registra as mensagens que já foram aplicadas ao banco de leitura,
/// evitando que o mesmo evento seja processado mais de uma vez.
/// </summary>
[Table("processed_messages")]
public sealed class ProcessedMessage
{
    private const int MaxEventTypeLength = 500;

    [Key]
    [Column("message_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid MessageId { get; set; }

    [Required]
    [MaxLength(MaxEventTypeLength)]
    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [Column("processed_at")]
    public DateTime ProcessedAt { get; set; }
}