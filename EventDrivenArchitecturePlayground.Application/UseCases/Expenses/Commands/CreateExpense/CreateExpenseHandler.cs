using EventDrivenArchitecturePlayground.Application.Abstractions.Messaging;
using EventDrivenArchitecturePlayground.Application.Abstractions.Persistence;
using EventDrivenArchitecturePlayground.Contracts.Events;
using EventDrivenArchitecturePlayground.Domain.Entities;
using EventDrivenArchitecturePlayground.Domain.Repositories;
using static EventDrivenArchitecturePlayground.Utils.Fixtures.Get;

namespace EventDrivenArchitecturePlayground.Application.UseCases.Expenses.Commands.CreateExpense;

/// <summary>
/// Executa o caso de uso responsável pelo registro de uma nova despesa.
/// 
/// O que esse handler basicamente faz:
/// 1. Recebe o command
/// 2. Cria a entidade Expense
/// 3.Adiciona Expense ao repositório
/// 4. Cria ExpenseCreatedIntegrationEvent
/// 5.Adiciona o evento ao Outbox
/// 6.Executa um único SaveChangesAsync
/// 7. Retorna o resultado
/// </summary>
public sealed class CreateExpenseHandler(
    IExpenseWriteRepository expenseRepository,
    IOutboxStore outboxStore,
    IUnitOfWork unitOfWork)
{
    private readonly IExpenseWriteRepository _expenseRepository = expenseRepository;
    private readonly IOutboxStore _outboxStore = outboxStore;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// Registra uma nova despesa e adiciona o evento correspondente
    /// ao Outbox dentro da mesma unidade de trabalho.
    /// </summary>
    /// <param name="command">
    /// Dados necessários para registrar a despesa.
    /// </param>
    /// <param name="cancellationToken">
    /// Token utilizado para cancelar a operação assíncrona.
    /// </param>
    /// <returns>Dados da despesa registrada.</returns>
    public async Task<CreateExpenseResult> HandleAsync(CreateExpenseCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // #3 - Cria a entidade de domínio (com validações).
        Expense expense = Expense.Create(
            command.Item,
            command.Amount,
            command.OccurredAt);

        // #4 - Adiciona a entidade ao contexto de persistência.
        await _expenseRepository.AddAsync(
            expense,
            cancellationToken);

        // 5 - Cria o evento de integração.
        ExpenseCreatedIntegrationEvent integrationEvent = new(
            EventId: Guid.NewGuid(),
            ExpenseId: expense.Id,
            Item: expense.Item,
            Amount: expense.Amount,
            ExpenseOccurredAt: expense.OccurredAt,
            OccurredOn: GetDate());

        // 6 - Adiciona o evento à Outbox.
        _outboxStore.Add(
            integrationEvent,
            routingKey: ExpenseCreatedIntegrationEvent.RoutingKey);

        // 7 - Persiste as alterações.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateExpenseResult(
            expense.Id,
            expense.Item,
            expense.Amount,
            expense.OccurredAt);
    }
}