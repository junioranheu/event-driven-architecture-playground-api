namespace EventDrivenArchitecturePlayground.Application.UseCases.Expenses.Queries.GetExpenses;

/// <summary>
/// Representa uma despesa retornada pelo lado
/// de leitura do CQRS.
/// </summary>
public sealed record GetExpensesResult(
    Guid Id,
    string Item,
    decimal Amount,
    DateTime OccurredAt);