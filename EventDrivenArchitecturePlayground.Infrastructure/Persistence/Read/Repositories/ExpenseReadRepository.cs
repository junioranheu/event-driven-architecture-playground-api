using EventDrivenArchitecturePlayground.Application.Abstractions.Persistence;
using EventDrivenArchitecturePlayground.Application.UseCases.Expenses.Queries.GetExpenses;
using Microsoft.EntityFrameworkCore;

namespace EventDrivenArchitecturePlayground.Infrastructure.Persistence.Read.Repositories;

/// <summary>
/// Consulta as projeções de despesas armazenadas
/// no banco de leitura do CQRS.
/// </summary>
public sealed class ExpenseReadRepository(ExpensesReadDbContext dbContext) : IExpenseReadRepository
{
    public async Task<IReadOnlyList<GetExpensesResult>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Consulta exclusivamente o banco Read.

        // AsNoTracking é utilizado porque os registros
        // serão apenas consultados e não modificados.
        List<GetExpensesResult> expenses = await dbContext.Expenses.
            AsNoTracking().
            OrderByDescending(x => x.OccurredAt).
            Select(x => new GetExpensesResult(
                x.Id,
                x.Item,
                x.Amount,
                x.OccurredAt)).
            ToListAsync(cancellationToken);

        return expenses;
    }
}