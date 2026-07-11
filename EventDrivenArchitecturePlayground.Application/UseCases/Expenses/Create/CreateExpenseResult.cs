namespace EventDrivenArchitecturePlayground.Application.UseCases.Expenses.Create;

/// <summary>
/// Representa o resultado retornado após o registro de uma despesa.
/// </summary>
/// <param name="Id">Identificador da despesa.</param>
/// <param name="Item">Nome ou descrição do item.</param>
/// <param name="Amount">Valor monetário registrado.</param>
/// <param name="OccurredAt">Data e hora em que a despesa ocorreu.</param>
/// Data e hora UTC em que o registro foi criado.
/// </param>
public sealed record CreateExpenseResult(
    Guid Id,
    string Item,
    decimal Amount,
    DateTimeOffset OccurredAt);