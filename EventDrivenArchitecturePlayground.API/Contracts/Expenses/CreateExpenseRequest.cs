namespace EventDrivenArchitecturePlayground.API.Contracts.Expenses;

/// <summary>
/// Representa os dados recebidos para registrar uma despesa.
/// </summary>
/// <param name="Item">Nome ou descrição da despesa.</param>
/// <param name="Amount">Valor monetário da despesa.</param>
/// <param name="OccurredAt">
/// Data e hora em que a despesa ocorreu.
/// </param>
public sealed record CreateExpenseRequest(string Item, decimal Amount, DateTime OccurredAt);