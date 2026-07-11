using EventDrivenArchitecturePlayground.Domain.Entities;
using EventDrivenArchitecturePlayground.Domain.Repositories;
using EventDrivenArchitecturePlayground.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventDrivenArchitecturePlayground.Infrastructure.Repositories;

public sealed class ExpenseRepository(ExpensesDbContext dbContext) : IExpenseRepository
{
    private readonly ExpensesDbContext _dbContext = dbContext;

    public async Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        await _dbContext.Expenses.AddAsync(expense, cancellationToken);
    }

    public Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Expenses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Expense>> GetByPeriodAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        List<Expense> expenses = await _dbContext.Expenses.
            AsNoTracking().
            Where(x => x.OccurredAt >= startDate.ToUniversalTime() && x.OccurredAt < endDate.ToUniversalTime()).
            OrderByDescending(expense => expense.OccurredAt).
            Skip(skip).
            Take(take).
            ToListAsync(cancellationToken);

        return expenses;
    }
}