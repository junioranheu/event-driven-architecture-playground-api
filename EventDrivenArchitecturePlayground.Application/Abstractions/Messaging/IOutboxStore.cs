using EventDrivenArchitecturePlayground.Contracts.Abstractions;

namespace EventDrivenArchitecturePlayground.Application.Abstractions.Messaging;

/// <summary>
/// Representa o armazenamento de eventos de integração no Outbox.
///
/// O Outbox é um armazenamento persistente onde os eventos são gravados na
/// mesma transação das alterações de negócio, garantindo que não sejam
/// perdidos caso a publicação no broker falhe.
///
/// Posteriormente, um processo dedicado lê esses eventos do Outbox e os
/// publica no sistema de mensageria.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Persiste um evento de integração no Outbox para publicação assíncrona.
    ///
    /// Este método apenas registra o evento no armazenamento do Outbox.
    /// A publicação no broker é realizada posteriormente pelo processador
    /// do Outbox.
    /// 
    /// <param name="integrationEvent">
    /// Evento de integração que deverá ser persistido para publicação futura.
    /// </param>
    /// <param name="routingKey">
    /// Rota de publicação do evento no broker.
    /// </param>
    void Add(IIntegrationEvent integrationEvent, string routingKey);
}