namespace EventDrivenArchitecturePlayground.Application.Abstractions.Persistence;

/// <summary>
/// Representa a unidade de trabalho responsável por confirmar
/// as alterações pendentes em uma única operação de persistência.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}