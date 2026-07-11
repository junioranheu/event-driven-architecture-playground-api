namespace EventDrivenArchitecturePlayground.API;

public static class DependencyInjectionApp
{
    /// <summary>
    /// Configura os middlewares que fazem parte do pipeline HTTP da aplicação.
    /// </summary>
    public static WebApplication UsePresentation(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            // Disponibiliza o documento OpenAPI em JSON.
            // Normalmente acessível em /swagger/v1/swagger.json.
            app.UseSwagger();

            // Disponibiliza a interface visual do Swagger
            // para visualizar e testar os endpoints.
            app.UseSwaggerUI();
        }

        // Redireciona requisições HTTP para HTTPS.
        app.UseHttpsRedirection();

        // Avalia as regras de autorização dos endpoints,
        // como atributos [Authorize] e políticas.
        app.UseAuthorization();

        // Mapeia as rotas declaradas nos Controllers,
        // como [Route], [HttpGet] e [HttpPost].
        app.MapControllers();

        return app;
    }
}