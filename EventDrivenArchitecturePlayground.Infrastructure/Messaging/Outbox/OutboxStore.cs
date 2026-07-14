using EventDrivenArchitecturePlayground.Application.Abstractions.Messaging;
using EventDrivenArchitecturePlayground.Contracts.Abstractions;
using EventDrivenArchitecturePlayground.Domain.Entities;
using EventDrivenArchitecturePlayground.Infrastructure.Persistence.Write;
using System.Text.Json;

namespace EventDrivenArchitecturePlayground.Infrastructure.Messaging.Outbox;

/// <summary>
/// Implementa o armazenamento de eventos de integração
/// utilizando o mesmo DbContext das entidades da aplicação.
/// </summary>
public sealed class OutboxStore(ExpensesWriteDbContext dbContext) : IOutboxStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public void Add(IIntegrationEvent integrationEvent, string routingKey)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);

        Type eventType = integrationEvent.GetType();

        string content = JsonSerializer.Serialize(
            value: integrationEvent,
            inputType: eventType,
            options: SerializerOptions);

        OutboxMessage outboxMessage = OutboxMessage.Create(
            id: integrationEvent.EventId,
            occurredOn: integrationEvent.OccurredOn,
            eventType: eventType.FullName ?? eventType.Name,
            routingKey: routingKey,
            content: content);

        dbContext.OutboxMessages.Add(outboxMessage);
    }
}