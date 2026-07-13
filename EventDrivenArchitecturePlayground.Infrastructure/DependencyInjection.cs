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
        AddMessaging(services, configuration);

        return services;
    }

    /// <summary>
    /// Registra e configura os serviços responsáveis
    /// pela persistência dos dados no PostgreSQL.
    /// </summary>
    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        // Mapeia a seção PostgreSql para PostgreSqlOptions
        // e valida a connection string durante a inicialização.
        services.AddOptions<PostgreSqlOptions>().
            Bind(configuration.GetSection(PostgreSqlOptions.SectionName)).
            Validate(x => IsValidPostgreSqlConnectionString(x.ConnectionString), "PostgreSql:ConnectionString must be a valid PostgreSQL connection string.").
            ValidateOnStart();

        // Registra o ExpensesDbContext como Scoped.
        // Uma nova instância será criada por requisição HTTP
        // ou por escopo criado pelo BackgroundService.
        services.AddDbContext<ExpensesDbContext>((serviceProvider, dbContextOptions) =>
        {
            // Obtém as configurações tipadas e já validadas
            // do PostgreSQL pelo container de dependências.
            PostgreSqlOptions postgreSqlOptions = serviceProvider.GetRequiredService<IOptions<PostgreSqlOptions>>().Value;

            // Configura o Entity Framework Core para utilizar
            // o PostgreSQL por meio do provider Npgsql.
            dbContextOptions.UseNpgsql(
                connectionString: postgreSqlOptions.ConnectionString,
                npgsqlOptionsAction: npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ExpensesDbContext).Assembly.FullName);
                });
        });

        // Faz IUnitOfWork apontar para a mesma instância Scoped
        // de ExpensesDbContext já criada no escopo atual.
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
    /// Registra os serviços relacionados ao processamento
    /// e publicação de eventos de integração.
    /// </summary>
    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OutboxPublisherOptions>().
            Bind(configuration.GetSection(OutboxPublisherOptions.SectionName)).
            Validate(x => x.PollingIntervalSeconds > 0, "OutboxPublisher:PollingIntervalSeconds must be greater than zero.").
            Validate(x => x.BatchSize > 0, "OutboxPublisher:BatchSize must be greater than zero.").
            Validate(x => x.MaxRetryDelaySeconds > 0, "OutboxPublisher:MaxRetryDelaySeconds must be greater than zero.").
            ValidateOnStart();

        // Utiliza o mesmo DbContext da requisição para armazenar
        // as mensagens do Outbox na mesma transação.
        services.AddScoped<IOutboxStore, OutboxStore>();

        // Processa um lote de mensagens pendentes utilizando
        // o DbContext do escopo atual.
        services.AddScoped<OutboxProcessor>();

        // Mantém uma única conexão e channel com o RabbitMQ
        // durante toda a vida da aplicação.
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

        // Cria a fila, o binding e inicia o consumidor
        // antes do Outbox começar a publicar mensagens.
        services.AddHostedService<RabbitMqConsumerHostedService>();

        // Consulta o Outbox e publica mensagens pendentes.
        services.AddHostedService<OutboxPublisherBackgroundService>();
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