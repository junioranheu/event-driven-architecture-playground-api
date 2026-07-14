using EventDrivenArchitecturePlayground.API.Contracts.Expenses;
using EventDrivenArchitecturePlayground.Application.UseCases.Expenses.Commands.CreateExpense;
using EventDrivenArchitecturePlayground.Application.UseCases.Expenses.Queries.GetExpenses;
using Microsoft.AspNetCore.Mvc;

namespace EventDrivenArchitecturePlayground.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ExpensesController(
    CreateExpenseHandler createExpenseHandler,
    GetExpensesHandler getExpensesHandler) : BaseController<ExpensesController>
{
    [HttpPost]
    [ProducesResponseType<CreateExpenseResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateExpenseResult>> CreateAsync([FromBody] CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        // #1 - Mapeia o request a um command.
        CreateExpenseCommand command = new(
            Item: request.Item,
            Amount: request.Amount,
            OccurredAt: request.OccurredAt);

        // #2 -  Inicia a execução do caso de uso.
        CreateExpenseResult result = await createExpenseHandler.HandleAsync(command, cancellationToken);

        // #8 - Retorna a resposta da API imediatamente (HTTP 201 Created).
        // A publicação do evento é realizada de forma assíncrona pelo bacgrkound service <see cref="OutboxPublisherBackgroundService"/>,
        // que processa a Outbox e envia a mensagem ao broker.
        return Created(string.Empty, result);
    }

    /// <summary>
    /// Retorna as despesas disponíveis no modelo
    /// de leitura do CQRS.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GetExpensesResult>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GetExpensesResult>>> GetAllAsync(CancellationToken cancellationToken)
    {
        GetExpensesQuery query = new();

        IReadOnlyList<GetExpensesResult> result = await getExpensesHandler.HandleAsync(query, cancellationToken);

        return Ok(result);
    }
}