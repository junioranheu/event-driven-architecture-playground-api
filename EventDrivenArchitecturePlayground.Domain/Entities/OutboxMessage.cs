using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventDrivenArchitecturePlayground.Domain.Entities;

/// <summary>
/// Representa um evento armazenado no banco e pendente
/// de publicação no RabbitMQ.
/// </summary>
/// [Table("outbox_messages")]
[Index(nameof(ProcessedAt), nameof(NextRetryAt), Name = "ix_outbox_messages_pending")]
public sealed class OutboxMessage
{
    private const int MaxEventTypeLength = 500;
    private const int MaxRoutingKeyLength = 255;

    private OutboxMessage(
        Guid id,
        DateTime occurred,
        string eventType,
        string routingKey,
        string content)
    {
        Id = id;
        OccurredOn = occurred;
        EventType = eventType;
        RoutingKey = routingKey;
        Content = content;
    }

    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; private set; }

    [Required]
    [Column("occurred_on")]
    public DateTime OccurredOn { get; private set; }

    [Required]
    [MaxLength(MaxEventTypeLength)]
    [Column("event_type")]
    public string EventType { get; private set; } = string.Empty;

    [Required]
    [MaxLength(MaxRoutingKeyLength)]
    [Column("routing_key")]
    public string RoutingKey { get; private set; } = string.Empty;

    [Required]
    [Column("content", TypeName = "jsonb")]
    public string Content { get; private set; } = string.Empty;

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; private set; }

    [Column("error", TypeName = "text")]
    public string? Error { get; private set; }

    [Required]
    [Column("retry_count")]
    public int RetryCount { get; private set; }

    [Column("next_retry_at")]
    public DateTime? NextRetryAt { get; private set; }

    /// <summary>
    /// Cria uma mensagem pendente no Outbox.
    /// </summary>
    public static OutboxMessage Create(
        Guid id,
        DateTime occurredOn,
        string eventType,
        string routingKey,
        string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new OutboxMessage(
            id,
            occurredOn,
            eventType,
            routingKey,
            content);
    }

    /// <summary>
    /// Marca a mensagem como publicada com sucesso.
    /// </summary>
    public void MarkAsProcessed(DateTime processedAt)
    {
        ProcessedAt = processedAt;
        Error = null;
        NextRetryAt = null;
    }

    /// <summary>
    /// Registra uma falha de publicação e agenda
    /// uma nova tentativa.
    /// </summary>
    public void MarkAsFailed(string error, DateTime nextRetryAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        RetryCount++;
        Error = error;
        NextRetryAt = nextRetryAt;
    }
}