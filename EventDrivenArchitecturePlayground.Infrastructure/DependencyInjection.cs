using EventDrivenArchitecturePlayground.Application.Abstractions.Messaging;
using EventDrivenArchitecturePlayground.Application.Abstractions.Persistence;
using EventDrivenArchitecturePlayground.Domain.Repositories;
using EventDrivenArchitecturePlayground.Infrastructure.Messaging.Outbox;
using EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq;
using EventDrivenArchitecturePlayground.Infrastructure.Persistence;
using EventDrivenArchitecturePlayground.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

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
        AddMessaging(services);

        return services;
    }

    /// <summary>
    /// Registra o Entity Framework Core e o PostgreSQL.
    /// </summary>
    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PostgreSqlOptions>().
            Bind(configuration.GetSection(PostgreSqlOptions.SectionName)).
            Validate(x => IsValidPostgreSqlConnectionString(x.ConnectionString), "PostgreSql:ConnectionString must be a valid PostgreSQL connection string.").
            ValidateOnStart();

        services.AddDbContext<ExpensesDbContext>((serviceProvider, dbContextOptions) =>
        {
            PostgreSqlOptions postgreSqlOptions = serviceProvider.GetRequiredService<IOptions<PostgreSqlOptions>>().Value;

            dbContextOptions.UseNpgsql(
                connectionString: postgreSqlOptions.ConnectionString,
                npgsqlOptionsAction: npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ExpensesDbContext).Assembly.FullName);
                });
        });

        services.AddScoped<IUnitOfWork>(x => x.GetRequiredService<ExpensesDbContext>());
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
            Validate(x => IsValidRabbitMqUrl(x.Url), "RabbitMQ:Url must be a valid amqp:// or amqps:// URL.").
            Validate(x => !string.IsNullOrWhiteSpace(x.ExchangeName), "RabbitMQ:ExchangeName is required.").
            ValidateOnStart();
    }

    /// <summary>
    /// Registra os serviços relacionados à mensageria.
    /// </summary>
    private static void AddMessaging(IServiceCollection services)
    {
        services.AddScoped<IOutboxStore, OutboxStore>();
    }

    /// <summary>
    /// Verifica se a string de conexão possui os dados mínimos
    /// necessários para conexão com o PostgreSQL.
    /// </summary>
    private static bool IsValidPostgreSqlConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        try
        {
            NpgsqlConnectionStringBuilder builder = new(connectionString);

            return !string.IsNullOrWhiteSpace(builder.Host) && 
                !string.IsNullOrWhiteSpace(builder.Database) && 
                !string.IsNullOrWhiteSpace(builder.Username);
        }
        catch (ArgumentException)
        {
            return false;
        }
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