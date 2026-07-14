using EventDrivenArchitecturePlayground.Application.UseCases.Expenses.Commands.CreateExpense;
using EventDrivenArchitecturePlayground.Application.UseCases.Expenses.Queries.GetExpenses;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenArchitecturePlayground.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        AddUseCases(services);

        return services;
    }

    #region extras
    private static void AddUseCases(IServiceCollection services)
    {
        services.AddScoped<CreateExpenseHandler>();
        services.AddScoped<GetExpensesHandler>();
    }
    #endregion
}