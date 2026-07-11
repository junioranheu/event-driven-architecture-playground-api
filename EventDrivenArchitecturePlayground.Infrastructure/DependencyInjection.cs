using EventDrivenArchitecturePlayground.Application.Abstractions.Persistence;
using EventDrivenArchitecturePlayground.Domain.Repositories;
using EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq;
using EventDrivenArchitecturePlayground.Infrastructure.Persistence;
using EventDrivenArchitecturePlayground.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenArchitecturePlayground.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra persistência, repositories, configurações
    /// e integrações externas da aplicação.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddPersistence(services, configuration);
        AddRepositories(services);
        AddRabbitMqOptions(services, configuration);

        return services;
    }

    /// <summary>
    /// Registra o Entity Framework Core e o PostgreSQL.
    /// </summary>
    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("PostgreSql") ??
            throw new InvalidOperationException("The PostgreSQL connection string was not configured.");

        services.AddDbContext<ExpensesDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ExpensesDbContext).Assembly.FullName);
                });
        });

        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<ExpensesDbContext>());
    }

    /// <summary>
    /// Registra as implementações dos repositories.
    /// </summary>
    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
    }

    /// <summary>
    /// Mapeia e valida as configurações do RabbitMQ.
    /// </summary>
    private static void AddRabbitMqOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>().
            Bind(configuration.GetSection(RabbitMqOptions.SectionName)).
            Validate(options => IsValidRabbitMqUrl(options.Url), "RabbitMQ:Url must be a valid amqp:// or amqps:// URL.").
            Validate(options => !string.IsNullOrWhiteSpace(options.ExchangeName), "RabbitMQ:ExchangeName is required.").
            ValidateOnStart();
    }

    /// <summary>
    /// Verifica se a URL utiliza um protocolo suportado pelo RabbitMQ.
    /// </summary>
    private static bool IsValidRabbitMqUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        return uri.Scheme.Equals("amqp", StringComparison.OrdinalIgnoreCase) ||
            uri.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase);
    }
}