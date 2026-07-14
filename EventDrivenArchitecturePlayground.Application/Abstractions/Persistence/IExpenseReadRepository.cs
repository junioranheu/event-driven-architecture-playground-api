using EventDrivenArchitecturePlayground.Application.UseCases.Expenses.Queries.GetExpenses;

namespace EventDrivenArchitecturePlayground.Application.Abstractions.Persistence;

/// <summary>
/// Define as consultas disponíveis no modelo
/// de leitura de despesas.
/// </summary>
public interface IExpenseReadRepository
{
    /// <summary>
    /// Retorna todas as despesas disponíveis
    /// no banco de leitura.
    /// </summary>
    Task<IReadOnlyList<GetExpensesResult>> GetAllAsync(CancellationToken cancellationToken = default);
}