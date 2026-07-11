namespace EventDrivenArchitecturePlayground.API;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os serviços necessários para a API.
    /// </summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        // Permite acessar informações da requisição HTTP atual,
        // como usuário autenticado, headers e IP.
        //
        // Outros serviços podem receber IHttpContextAccessor
        // por injeção de dependência.
        services.AddHttpContextAccessor();

        // Registra o suporte a Controllers do ASP.NET Core,
        // incluindo model binding, validação e respostas HTTP.
        services.AddControllers();

        // Registra o API Explorer, usado para descobrir
        // os endpoints disponíveis na aplicação.
        //
        // Essas informações são utilizadas pelo Swagger.
        services.AddEndpointsApiExplorer();

        // Registra o gerador da documentação OpenAPI/Swagger.
        services.AddSwaggerGen();

        return services;
    }
}