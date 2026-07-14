namespace EventDrivenArchitecturePlayground.Application.UseCases.Expenses.Commands.CreateExpense;

/// <summary>
/// Representa os dados necessários para registrar uma nova despesa.
/// </summary>
/// <param name="Item">Nome ou descrição do item.</param>
/// <param name="Amount">Valor monetário da despesa.</param>
/// <param name="OccurredAt">
/// Data e hora em que a despesa ocorreu.
/// </param>
public sealed record CreateExpenseCommand(
    string Item,
    decimal Amount,
    DateTime OccurredAt);