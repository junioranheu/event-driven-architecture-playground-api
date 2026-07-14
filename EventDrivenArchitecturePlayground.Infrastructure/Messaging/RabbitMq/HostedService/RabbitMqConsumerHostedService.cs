using EventDrivenArchitecturePlayground.Contracts.Events;
using EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq.Options;
using EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq.Projections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq.HostedService;

/// <summary>
/// Declara a fila da aplicação e consome as mensagens
/// publicadas no exchange do RabbitMQ.
/// </summary>
public sealed class RabbitMqConsumerHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConsumerHostedService> logger) : IHostedService, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private IConnection? _connection;
    private IChannel? _channel;

    /// <summary>
    /// Configura a conexão, a fila, o binding
    /// e inicia o consumo das mensagens.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ConnectionFactory connectionFactory = new()
        {
            Uri = new Uri(_options.Url),
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            ClientProvidedName = "event-driven-playground:consumer"
        };

        _connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        IChannel channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        _channel = channel;

        // Garante que o exchange utilizado pelo publisher exista.
        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        // Cria uma única fila para esta aplicação.
        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        // Define quais routing keys serão encaminhadas
        // pelo exchange para esta fila.
        await channel.QueueBindAsync(
            queue: _options.QueueName,
            exchange: _options.ExchangeName,
            routingKey: _options.BindingKey,
            arguments: null,
            cancellationToken: cancellationToken);

        AsyncEventingBasicConsumer consumer = new(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            // #12 - Registra o comportamento que será executado sempre que
            // uma nova mensagem for entregue pelo RabbitMQ ao consumer.
            byte[] body = eventArgs.Body.ToArray();
            string content = Encoding.UTF8.GetString(body);

            try
            {
                // logger.LogInformation("RabbitMQ message received. Routing key: {RoutingKey}. Content: {Content}", eventArgs.RoutingKey, content);

                // #13 - Identifica o tipo do evento pela routing key
                // e direciona a mensagem para o respectivo processamento.
                //
                // Novos eventos podem ser adicionados dentro desse método,
                // cada um com sua própria condição e handler.
                await ProcessMessageAsync(eventArgs, content);

                // Confirma o processamento e remove
                // a mensagem da fila.
                await channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to process RabbitMQ message {MessageId}.", eventArgs.BasicProperties.MessageId);

                // Devolve a mensagem para a fila.
                await channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true);
            }
        };

        // Inicia o consumo com confirmação manual.
        await channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        // logger.LogInformation("RabbitMQ consumer started. Queue: {QueueName}. Binding key: {BindingKey}.", _options.QueueName, _options.BindingKey);
    }

    #region extras
    /// <summary>
    /// Identifica o evento recebido e executa
    /// seu respectivo handler de projeção.
    /// </summary>
    private async Task ProcessMessageAsync(BasicDeliverEventArgs eventArgs, string content)
    {
        if (!Guid.TryParse(eventArgs.BasicProperties.MessageId, out Guid messageId))
        {
            throw new InvalidOperationException("RabbitMQ message does not contain a valid MessageId.");
        }

        // Converte o conteúdo JSON recebido do RabbitMQ
        // novamente para o tipo concreto do evento.
        //
        // Caso o conteúdo seja inválido ou incompatível,
        // interrompe o processamento para que a mensagem não receba ACK.
        ExpenseCreatedIntegrationEvent integrationEvent =
            JsonSerializer.Deserialize<ExpenseCreatedIntegrationEvent>(content, _jsonOptions) ??
            throw new InvalidOperationException("ExpenseCreatedIntegrationEvent could not be deserialized.");

        // Cria um escopo próprio para que o handler
        // receba um ExpensesReadDbContext Scoped.
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        switch (eventArgs.RoutingKey)
        {
            case ExpenseCreatedIntegrationEvent.RoutingKey:
                {
                    ExpenseCreatedProjectionHandler handler = scope.ServiceProvider.GetRequiredService<ExpenseCreatedProjectionHandler>();
                    await handler.HandleAsync(messageId, integrationEvent);
                    break;
                }

            default:
                throw new InvalidOperationException($"No projection handler was found for routing key '{eventArgs.RoutingKey}'.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
    #endregion
}