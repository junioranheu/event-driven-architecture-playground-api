using EventDrivenArchitecturePlayground.API.Contracts.Expenses;
using EventDrivenArchitecturePlayground.Application.UseCases.Expenses.Create;
using Microsoft.AspNetCore.Mvc;

namespace EventDrivenArchitecturePlayground.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ExpensesController(CreateExpenseHandler createExpenseHandler) : BaseController<ExpensesController>
{
    [HttpPost]
    [ProducesResponseType<CreateExpenseResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateExpenseResult>> CreateAsync([FromBody] CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        CreateExpenseCommand command = new(
            Item: request.Item,
            Amount: request.Amount,
            OccurredAt: request.OccurredAt);

        CreateExpenseResult result =
            await createExpenseHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(CreateAsync), new { id = result.Id }, result);
    }
}