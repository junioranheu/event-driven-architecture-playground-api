using EventDrivenArchitecturePlayground.Domain.Entities;
using EventDrivenArchitecturePlayground.Domain.Repositories;
using EventDrivenArchitecturePlayground.Infrastructure.Persistence.Write;
using Microsoft.EntityFrameworkCore;

namespace EventDrivenArchitecturePlayground.Infrastructure.Repositories;

public sealed class ExpenseRepository(ExpensesWriteDbContext dbContext) : IExpenseRepository
{
    private readonly ExpensesWriteDbContext _dbContext = dbContext;

    public async Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        await _dbContext.Expenses.AddAsync(expense, cancellationToken);
    }

    public Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Expenses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Expense>> GetByPeriodAsync(
        DateTime startDate,
        DateTime endDate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        List<Expense> expenses = await _dbContext.Expenses.
            AsNoTracking().
            Where(x => x.OccurredAt >= startDate && x.OccurredAt < endDate).
            OrderByDescending(expense => expense.OccurredAt).
            Skip(skip).
            Take(take).
            ToListAsync(cancellationToken);

        return expenses;
    }
}