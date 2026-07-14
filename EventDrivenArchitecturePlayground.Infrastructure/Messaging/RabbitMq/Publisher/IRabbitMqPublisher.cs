namespace EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq.Publisher;

/// <summary>
/// Define a publicação de mensagens no RabbitMQ.
/// </summary>
public interface IRabbitMqPublisher
{
    /// <summary>
    /// Publica uma mensagem serializada no RabbitMQ.
    /// </summary>
    Task PublishAsync(
        Guid messageId,
        string eventType,
        string routingKey,
        string content,
        CancellationToken cancellationToken = default);
}