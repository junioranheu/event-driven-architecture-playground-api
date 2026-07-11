using EventDrivenArchitecturePlayground.Domain.Entities;

namespace EventDrivenArchitecturePlayground.Domain.Repositories;

/// <summary>
/// Por que a interface fica no Domain?
/// 
/// O domínio define aquilo de que precisa.
/// Mas ele não sabe como os dados serão persistidos.
/// A implementação é criada na camada Infrastructure.
/// Nesse caso, está sendo usado PostgreSQL e Entity Framework.
/// 
/// Sendo assim, o domínio não depende de nenhuma tecnologia específica de persistência.
/// </summary>
public interface IExpenseRepository
{
    Task AddAsync(Expense expense, CancellationToken cancellationToken = default);

    Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Expense>> GetByPeriodAsync(
        DateTime startDate,
        DateTime endDate,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}