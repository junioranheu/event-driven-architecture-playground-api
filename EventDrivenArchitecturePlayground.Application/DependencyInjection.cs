using EventDrivenArchitecturePlayground.Application.UseCases.Expenses.Create;
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
    }
    #endregion
}