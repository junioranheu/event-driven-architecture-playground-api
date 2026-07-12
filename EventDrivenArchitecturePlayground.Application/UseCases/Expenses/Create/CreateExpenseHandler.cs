using EventDrivenArchitecturePlayground.Application.Abstractions.Messaging;
using EventDrivenArchitecturePlayground.Application.Abstractions.Persistence;
using EventDrivenArchitecturePlayground.Contracts.Events;
using EventDrivenArchitecturePlayground.Domain.Entities;
using EventDrivenArchitecturePlayground.Domain.Repositories;
using static EventDrivenArchitecturePlayground.Utils.Fixtures.Get;

namespace EventDrivenArchitecturePlayground.Application.UseCases.Expenses.Create;

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
    IExpenseRepository expenseRepository,
    IOutboxStore outboxStore,
    IUnitOfWork unitOfWork)
{
    private readonly IExpenseRepository _expenseRepository = expenseRepository;
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

        Expense expense = Expense.Create(
            command.Item,
            command.Amount,
            command.OccurredAt);

        await _expenseRepository.AddAsync(
            expense,
            cancellationToken);

        ExpenseCreatedIntegrationEvent integrationEvent = new(
            EventId: Guid.NewGuid(),
            ExpenseId: expense.Id,
            Item: expense.Item,
            Amount: expense.Amount,
            ExpenseOccurredAt: expense.OccurredAt,
            OccurredOn: GetDate());

        _outboxStore.Add(
            integrationEvent,
            routingKey: ExpenseCreatedIntegrationEvent.RoutingKey);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateExpenseResult(
            expense.Id,
            expense.Item,
            expense.Amount,
            expense.OccurredAt);
    }
}