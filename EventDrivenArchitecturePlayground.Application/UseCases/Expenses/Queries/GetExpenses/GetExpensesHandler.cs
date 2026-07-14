using EventDrivenArchitecturePlayground.Application.Abstractions.Persistence;

namespace EventDrivenArchitecturePlayground.Application.UseCases.Expenses.Queries.GetExpenses;

/// <summary>
/// Executa a consulta de despesas utilizando
/// exclusivamente o modelo de leitura do CQRS.
/// </summary>
public sealed class GetExpensesHandler(IExpenseReadRepository expenseReadRepository)
{
    /// <summary>
    /// Retorna todas as despesas disponíveis
    /// no banco de leitura.
    /// </summary>
    public async Task<IReadOnlyList<GetExpensesResult>> HandleAsync(GetExpensesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        IReadOnlyList<GetExpensesResult> expenses = await expenseReadRepository.GetAllAsync(cancellationToken);

        return expenses;
    }
}