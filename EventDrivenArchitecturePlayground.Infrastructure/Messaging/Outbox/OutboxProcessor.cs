using EventDrivenArchitecturePlayground.Domain.Entities;
using EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq.Publisher;
using EventDrivenArchitecturePlayground.Infrastructure.Persistence.Write;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static EventDrivenArchitecturePlayground.Utils.Fixtures.Get;

namespace EventDrivenArchitecturePlayground.Infrastructure.Messaging.Outbox;

/// <summary>
/// Busca mensagens pendentes no Outbox e as publica no RabbitMQ.
/// </summary>
public sealed class OutboxProcessor(
    ExpensesWriteDbContext dbContext,
    IRabbitMqPublisher rabbitMqPublisher,
    IOptions<OutboxPublisherOptions> options,
    ILogger<OutboxProcessor> logger)
{
    private readonly OutboxPublisherOptions _options = options.Value;

    /// <summary>
    /// Processa um lote de mensagens pendentes e retorna
    /// a quantidade de mensagens encontradas.
    /// </summary>
    public async Task<int> ProcessAsync(CancellationToken cancellationToken = default)
    {
        List<OutboxMessage> messages = await dbContext.OutboxMessages.
            Where(x =>
                x.ProcessedAt == null &&
                (
                    x.NextRetryAt == null ||
                    x.NextRetryAt <= GetDate()
                )).
            OrderBy(x => x.OccurredOn).
            Take(_options.BatchSize).
            ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return 0;
        }

        foreach (OutboxMessage message in messages)
        {
            await ProcessMessageAsync(message, cancellationToken);
        }

        return messages.Count;
    }

    #region extras
    /// <summary>
    /// Publica e atualiza o estado de uma mensagem do Outbox.
    /// </summary>
    private async Task ProcessMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            // #10 - Tenta publicar a mensagem no RabbitMQ.
            await rabbitMqPublisher.PublishAsync(
                messageId: message.Id,
                eventType: message.EventType,
                routingKey: message.RoutingKey,
                content: message.Content,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await RegisterFailureAsync(message, exception, cancellationToken);
            return;
        }

        // Só marca como processada quando a publicação
        // no RabbitMQ foi concluída com sucesso.
        message.MarkAsProcessed(processedAt: GetDate());

        // Uma falha neste SaveChanges não deve ser tratada
        // como falha de publicação no RabbitMQ.
        await dbContext.SaveChangesAsync(cancellationToken);

        // logger.LogInformation("Outbox message {MessageId} processed successfully.", message.Id);
    }

    /// <summary>
    /// Registra a falha de publicação e agenda uma nova tentativa.
    /// </summary>
    private async Task RegisterFailureAsync(OutboxMessage message, Exception exception, CancellationToken cancellationToken)
    {
        DateTime nextRetryAt = CalculateNextRetry(
            currentRetryCount: message.RetryCount,
            currentDate: GetDate());

        message.MarkAsFailed(
            error: exception.Message,
            nextRetryAt);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogError(exception, "Failed to publish outbox message {MessageId}. Next attempt at {NextRetryAt}.", message.Id, nextRetryAt);
    }

    /// <summary>
    /// Calcula a próxima tentativa utilizando backoff exponencial.
    /// </summary>
    private DateTime CalculateNextRetry(int currentRetryCount, DateTime currentDate)
    {
        int exponent = Math.Min(currentRetryCount + 1, 20);

        int calculatedDelaySeconds = (int)Math.Pow(2, exponent);

        int delaySeconds = Math.Min(calculatedDelaySeconds, _options.MaxRetryDelaySeconds);

        return currentDate.AddSeconds(delaySeconds);
    }
    #endregion
}