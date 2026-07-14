using EventDrivenArchitecturePlayground.Infrastructure.Persistence.Read.Models;
using Microsoft.EntityFrameworkCore;

namespace EventDrivenArchitecturePlayground.Infrastructure.Persistence.Read;

/// <summary>
/// Contexto responsável exclusivamente pelo banco
/// utilizado nas consultas do lado de leitura do CQRS.
/// </summary>
public sealed class ExpensesReadDbContext(DbContextOptions<ExpensesReadDbContext> options) : DbContext(options)
{
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<ExpenseReadModel> Expenses => Set<ExpenseReadModel>();
}