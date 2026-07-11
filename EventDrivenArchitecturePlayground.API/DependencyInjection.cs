namespace EventDrivenArchitecturePlayground.API;

/// <summary>
/// Centraliza a configuração dos serviços e middlewares
/// pertencentes à camada de apresentação.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra os serviços necessários para a API.
    /// </summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    /// <summary>
    /// Configura o pipeline HTTP da aplicação.
    /// </summary>
    public static WebApplication UsePresentation(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}