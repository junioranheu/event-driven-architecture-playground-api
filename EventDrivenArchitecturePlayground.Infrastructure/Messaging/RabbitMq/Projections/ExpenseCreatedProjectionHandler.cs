using EventDrivenArchitecturePlayground.Contracts.Events;
using EventDrivenArchitecturePlayground.Infrastructure.Persistence.Read;
using EventDrivenArchitecturePlayground.Infrastructure.Persistence.Read.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static EventDrivenArchitecturePlayground.Utils.Fixtures.Get;

namespace EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq.Projections;

/// <summary>
/// Aplica o evento de criação de despesa
/// ao modelo de leitura do CQRS.
/// </summary>
public sealed class ExpenseCreatedProjectionHandler(ExpensesReadDbContext dbContext, ILogger<ExpenseCreatedProjectionHandler> logger)
{
    /// <summary>
    /// Insere ou atualiza a despesa no banco de leitura
    /// e registra a mensagem como processada.
    /// </summary>
    public async Task HandleAsync(
        Guid messageId,
        ExpenseCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
        }

        ArgumentNullException.ThrowIfNull(integrationEvent);

        // Verifica se esta mensagem já foi aplicada ao banco Read.
        bool alreadyProcessed = await dbContext.ProcessedMessages.
            AnyAsync(x => x.MessageId == messageId, cancellationToken);

        if (alreadyProcessed)
        {
            // logger.LogInformation("RabbitMQ message {MessageId} was already projected.", messageId);
            return;
        }

        ExpenseReadModel? readModel = await dbContext.Expenses.
            SingleOrDefaultAsync(x => x.Id == integrationEvent.ExpenseId, cancellationToken);

        if (readModel is null)
        {
            // Cria a projeção quando a despesa ainda
            // não existe no banco de leitura.
            readModel = new ExpenseReadModel
            {
                Id = integrationEvent.ExpenseId,
                Item = integrationEvent.Item,
                Amount = integrationEvent.Amount,
                OccurredAt = integrationEvent.OccurredOn
            };

            dbContext.Expenses.Add(readModel);
        }
        else
        {
            // Atualiza a projeção caso o evento seja reaplicado
            // ou o modelo já exista por causa de um resync.
            readModel.Item = integrationEvent.Item;
            readModel.Amount = integrationEvent.Amount;
            readModel.OccurredAt = integrationEvent.OccurredOn;
        }

        // Registra a mensagem para garantir idempotência.
        dbContext.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = messageId,
            EventType = nameof(ExpenseCreatedIntegrationEvent),
            ProcessedAt = GetDate()
        });

        // A projeção e o registro da mensagem são confirmados
        // juntos na mesma transação do banco de leitura.
        await dbContext.SaveChangesAsync(cancellationToken);

        // logger.LogInformation("Expense {ExpenseId} projected successfully from message {MessageId}.", integrationEvent.ExpenseId, messageId);
    }
}