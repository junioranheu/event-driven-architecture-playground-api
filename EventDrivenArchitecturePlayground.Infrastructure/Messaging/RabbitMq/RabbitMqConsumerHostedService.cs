using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Declara a fila da aplicação e consome as mensagens
/// publicadas no exchange do RabbitMQ.
/// </summary>
public sealed class RabbitMqConsumerHostedService(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConsumerHostedService> logger) : IHostedService, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;

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
            byte[] body = eventArgs.Body.ToArray();

            string content = Encoding.UTF8.GetString(body);

            try
            {
                // Por enquanto, apenas comprova que a mensagem
                // foi recebida pelo consumidor.
                // logger.LogInformation("RabbitMQ message received. Routing key: {RoutingKey}. Content: {Content}", eventArgs.RoutingKey, content);

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
}