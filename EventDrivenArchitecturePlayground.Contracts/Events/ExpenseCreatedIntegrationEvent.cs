using EventDrivenArchitecturePlayground.Contracts.Abstractions;

namespace EventDrivenArchitecturePlayground.Contracts.Events;

/// <summary>
/// Representa o evento publicado quando uma nova despesa é registrada.
///
/// #1 - Mas, por que usar record?
/// O evento representa um fato que já aconteceu:
/// Uma despesa foi criada.
/// Depois de publicado, esse evento não deve ser alterado.
/// O record combina bem com esse tipo de objeto porque oferece:
/// 
/// imutabilidade;
/// comparação por valor;
/// sintaxe menor;
/// boa serialização;
/// clareza para objetos de transporte.
/// 
/// #2 - Por que o evento não recebe a entidade Expense?
/// "public sealed record ExpenseCreatedIntegrationEvent(Expense Expense);"
/// 
/// Porque isso faria o projeto Expenses.Contracts depender do Expenses.Domain.
/// Além disso, expor a entidade diretamente poderia publicar:
/// propriedades internas;
/// campos adicionados futuramente;
/// informações que o consumidor não precisa;
/// estruturas difíceis de versionar.
/// </summary>
public sealed record ExpenseCreatedIntegrationEvent(
    Guid EventId,
    Guid ExpenseId,
    string Item,
    decimal Amount,
    DateTime ExpenseOccurredAt,
    DateTime OccurredOn) : IIntegrationEvent
{
    /// <summary>
    /// Routing key utilizada para identificar a versão deste evento no RabbitMQ.
    /// </summary>
    public const string RoutingKey = "expenses.expense-created.v1";
}