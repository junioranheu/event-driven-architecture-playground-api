using EventDrivenArchitecturePlayground.Infrastructure.Serialization;
using System.Text.Json.Serialization;

namespace EventDrivenArchitecturePlayground.API;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os serviços necessários para a API.
    /// </summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services, WebApplicationBuilder builder)
    {
        IWebHostEnvironment env = builder.Environment;

        // Permite acessar informações da requisição HTTP atual,
        // como usuário autenticado, headers e IP.
        //
        // Outros serviços podem receber IHttpContextAccessor
        // por injeção de dependência.
        services.AddHttpContextAccessor();

        // Registra o suporte a Controllers do ASP.NET Core,
        // incluindo model binding, validação e respostas HTTP.
        services.AddControllers().
            AddJsonOptions(x =>
            {
                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                x.JsonSerializerOptions.WriteIndented = env.IsDevelopment();
                x.JsonSerializerOptions.Converters.Add(new BrasiliaDateTimeConverter());
            });

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