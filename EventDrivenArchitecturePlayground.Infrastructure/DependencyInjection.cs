using EventDrivenArchitecturePlayground.Application.Abstractions.Messaging;
using EventDrivenArchitecturePlayground.Application.Abstractions.Persistence;
using EventDrivenArchitecturePlayground.Domain.Repositories;
using EventDrivenArchitecturePlayground.Infrastructure.Messaging.Outbox;
using EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq.HostedService;
using EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq.Options;
using EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq.Projections;
using EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq.Publisher;
using EventDrivenArchitecturePlayground.Infrastructure.Persistence.Read;
using EventDrivenArchitecturePlayground.Infrastructure.Persistence.Read.Options;
using EventDrivenArchitecturePlayground.Infrastructure.Persistence.Write;
using EventDrivenArchitecturePlayground.Infrastructure.Persistence.Write.Options;
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
        // Mapeia e valida as configurações do PostgreSQL
        // utilizado pelo lado de escrita da aplicação.
        services.AddOptions<PostgreSqlWriteOptions>().
            Bind(configuration.GetSection(PostgreSqlWriteOptions.SectionName)).
            Validate(x => IsValidPostgreSqlConnectionString(x.ConnectionString), "PostgreSqlWrite:ConnectionString must be a valid PostgreSQL connection string.").
            ValidateOnStart();

        // Mapeia e valida as configurações do PostgreSQL
        // que será utilizado pelo lado de leitura do CQRS.
        services.AddOptions<PostgreSqlReadOptions>().
            Bind(configuration.GetSection(PostgreSqlReadOptions.SectionName)).
            Validate(x => IsValidPostgreSqlConnectionString(x.ConnectionString), "PostgreSqlRead:ConnectionString must be a valid PostgreSQL connection string.").
            ValidateOnStart();

        // Banco de escrita: entidades de domínio e Outbox.
        services.AddDbContext<ExpensesWriteDbContext>((serviceProvider, dbContextOptions) =>
        {
            PostgreSqlWriteOptions postgreSqlOptions = serviceProvider.GetRequiredService<IOptions<PostgreSqlWriteOptions>>().Value;

            dbContextOptions.UseNpgsql(
                connectionString: postgreSqlOptions.ConnectionString,
                npgsqlOptionsAction: npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ExpensesWriteDbContext).Assembly.FullName);
                });
        });

        // Banco de leitura: modelos otimizados para consultas.
        services.AddDbContext<ExpensesReadDbContext>(
        (serviceProvider, dbContextOptions) =>
        {
            PostgreSqlReadOptions postgreSqlOptions = serviceProvider.GetRequiredService<IOptions<PostgreSqlReadOptions>>().Value;

            dbContextOptions.UseNpgsql(
                connectionString: postgreSqlOptions.ConnectionString,
                npgsqlOptionsAction: npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ExpensesReadDbContext).Assembly.FullName);
                });
        });


        // Faz o IUnitOfWork utilizar a mesma instância Scoped
        // do ExpensesDbContext usada pelos repositories e OutboxStore.
        services.AddScoped<IUnitOfWork>(x => x.GetRequiredService<ExpensesWriteDbContext>());
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

        // Aplica eventos de criação de despesas ao banco de leitura do CQRS.
        services.AddScoped<ExpenseCreatedProjectionHandler>();
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