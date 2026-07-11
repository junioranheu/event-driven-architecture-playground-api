using EventDrivenArchitecturePlayground.Infrastructure.Serialization;
using System.Text.Json.Serialization;

namespace EventDrivenArchitecturePlayground.API;

public static class DependencyInjection
{
    private const string CorsPolicyName = "Frontend";

    /// <summary>
    /// Registra os serviços necessários para a API.
    /// </summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services, WebApplicationBuilder builder)
    {
        IWebHostEnvironment env = builder.Environment;
        ConfigurationManager configuration = builder.Configuration;

        // Configura o CORS para permitir requisições do front-end;
        AddCors(services, configuration);

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

    /// <summary>
    /// Registra a política de CORS utilizada pelos frontends.
    /// </summary>
    private static void AddCors(IServiceCollection services, ConfigurationManager configuration)
    {
        string[] allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy.AllowAnyHeader().AllowAnyMethod();

                bool allowAnyOrigin = allowedOrigins.Contains("*");

                if (allowAnyOrigin)
                {
                    policy.AllowAnyOrigin();
                    return;
                }

                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins);
                }
            });
        });
    }
}