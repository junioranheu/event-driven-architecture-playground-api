using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;

namespace EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Publica mensagens no RabbitMQ utilizando uma conexão
/// e um channel de longa duração.
/// </summary>
public sealed class RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger) : IRabbitMqPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _channelSemaphore = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(
        Guid messageId,
        string eventType,
        string routingKey,
        string content,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        await _channelSemaphore.WaitAsync(cancellationToken);

        try
        {
            await EnsureConnectedAsync(cancellationToken);

            byte[] body = Encoding.UTF8.GetBytes(content);

            BasicProperties properties = new()
            {
                MessageId = messageId.ToString(),
                Type = eventType,
                ContentType = "application/json",
                DeliveryMode = 2
            };

            await _channel.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogInformation("Message {MessageId} published with routing key {RoutingKey}.", messageId, routingKey);
        }
        catch
        {
            await DisposeRabbitMqResourcesAsync();
            throw;
        }
        finally
        {
            _channelSemaphore.Release();
        }
    }

    /// <summary>
    /// Garante que a conexão, o channel e o exchange estejam disponíveis.
    /// </summary>
    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
        {
            return;
        }

        await DisposeRabbitMqResourcesAsync();

        ConnectionFactory connectionFactory = new()
        {
            Uri = new Uri(_options.Url),
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            ClientProvidedName = "event-driven-playground:outbox-publisher"
        };

        _connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        CreateChannelOptions channelOptions = new(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        _channel = await _connection.CreateChannelAsync(
            channelOptions,
            cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Libera os recursos utilizados pelo RabbitMQ.
    /// </summary>
    private async Task DisposeRabbitMqResourcesAsync()
    {
        if (_channel is not null)
        {
            try
            {
                if (_channel.IsOpen)
                {
                    await _channel.CloseAsync();
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "An error occurred while closing the RabbitMQ channel.");
            }

            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            try
            {
                if (_connection.IsOpen)
                {
                    await _connection.CloseAsync();
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "An error occurred while closing the RabbitMQ connection.");
            }

            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _channelSemaphore.WaitAsync();

        try
        {
            await DisposeRabbitMqResourcesAsync();
        }
        finally
        {
            _channelSemaphore.Release();
            _channelSemaphore.Dispose();
        }
    }
}