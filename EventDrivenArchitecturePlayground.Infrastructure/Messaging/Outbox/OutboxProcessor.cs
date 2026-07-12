using EventDrivenArchitecturePlayground.Domain.Entities;
using EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq;
using EventDrivenArchitecturePlayground.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static EventDrivenArchitecturePlayground.Utils.Fixtures.Get;

namespace EventDrivenArchitecturePlayground.Infrastructure.Messaging.Outbox;

/// <summary>
/// Busca mensagens pendentes no Outbox e as publica no RabbitMQ.
/// </summary>
public sealed class OutboxProcessor(
    ExpensesDbContext dbContext,
    IRabbitMqPublisher rabbitMqPublisher,
    IOptions<OutboxPublisherOptions> options,
    ILogger<OutboxProcessor> logger)
{
    private readonly OutboxPublisherOptions _options = options.Value;

    /// <summary>
    /// Processa um lote de mensagens pendentes.
    /// </summary>
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        // Busca um lote de mensagens pendentes no Outbox.
        //
        // Uma mensagem será selecionada quando:
        // 1. Ainda não tiver sido processada com sucesso;
        // 2. Nunca tiver falhado antes ou já tiver atingido
        //    a data programada para uma nova tentativa.
        //
        // As mensagens mais antigas são processadas primeiro
        // e a quantidade retornada é limitada pelo tamanho do lote configurado.
        List<OutboxMessage> messages = await dbContext.OutboxMessages.
            Where(x =>
                x.ProcessedAt == null &&
                (x.NextRetryAt == null || x.NextRetryAt <= GetDate())
            ).
            OrderBy(message => message.OccurredOn).
            Take(_options.BatchSize).
            ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (OutboxMessage message in messages)
        {
            try
            {
                // Publica no RabbitMQ o conteúdo armazenado no Outbox,
                // mantendo o identificador, o tipo do evento e a routing key
                // originais da mensagem.
                await rabbitMqPublisher.PublishAsync(
                    message.Id,
                    message.EventType,
                    message.RoutingKey,
                    message.Content,
                    cancellationToken);

                // Marca a mensagem como processada somente após
                // a publicação ser concluída com sucesso.
                message.MarkAsProcessed(GetDate());

                // Persiste no banco a atualização do estado da mensagem,
                // evitando que ela seja publicada novamente no próximo ciclo.
                await dbContext.SaveChangesAsync(cancellationToken);

                // logger.LogInformation("Outbox message {MessageId} processed successfully.", message.Id);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Calcula a data e hora da próxima tentativa
                // com base na quantidade de falhas já registradas.
                DateTime nextRetryAt = CalculateNextRetry(message.RetryCount);

                // Registra a falha na mensagem, incrementa o número
                // de tentativas e define quando ela poderá ser processada novamente.
                message.MarkAsFailed(exception.Message, nextRetryAt);

                // Persiste no PostgreSQL o erro, a nova contagem de tentativas
                // e a data agendada para o próximo retry.
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogError(exception, "Failed to publish outbox message {MessageId}. Next attempt at {NextRetryAtUtc}.", message.Id, nextRetryAt);
            }
        }
    }

    #region extras
    /// <summary>
    /// Calcula a próxima tentativa utilizando backoff exponencial.
    /// </summary>
    private DateTime CalculateNextRetry(int currentRetryCount)
    {
        int calculatedDelaySeconds = (int)Math.Pow(2, Math.Min(currentRetryCount + 1, 20));

        int delaySeconds = Math.Min(calculatedDelaySeconds, _options.MaxRetryDelaySeconds);

        DateTime output = GetDate().AddSeconds(delaySeconds);

        return output;
    }
    #endregion
}