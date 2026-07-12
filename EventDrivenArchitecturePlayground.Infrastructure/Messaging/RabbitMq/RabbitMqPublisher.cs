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

    // Impede que duas publicações utilizem o mesmo channel simultaneamente.
    private readonly SemaphoreSlim _channelSemaphore = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    /// <summary>
    /// Publica uma mensagem no exchange configurado.
    /// </summary>
    public async Task PublishAsync(
        Guid messageId,
        string eventType,
        string routingKey, 
        string content,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        await _channelSemaphore.WaitAsync(cancellationToken);

        try
        {
            // Retorna o channel existente quando estiver aberto
            // ou cria uma nova conexão e um novo channel.
            IChannel channel = await GetOrCreateChannelAsync(cancellationToken);

            byte[] body = Encoding.UTF8.GetBytes(content);

            BasicProperties properties = new()
            {
                MessageId = messageId.ToString(),
                Type = eventType,
                ContentType = "application/json",

                // Define a mensagem como persistente.
                // Equivale ao DeliveryMode 2.
                Persistent = true
            };

            // Publica a mensagem e aguarda a confirmação do RabbitMQ.
            await channel.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            // logger.LogInformation("Message {MessageId} published with routing key {RoutingKey}.", messageId, routingKey);
        }
        catch
        {
            // Descarta os recursos atuais para que uma nova conexão
            // seja criada na próxima tentativa do Outbox.
            await DisposeRabbitMqResourcesAsync();

            throw;
        }
        finally
        {
            _channelSemaphore.Release();
        }
    }

    /// <summary>
    /// Retorna o channel atual quando ele estiver aberto
    /// ou cria uma nova conexão, um novo channel e o exchange.
    /// </summary>
    private async Task<IChannel> GetOrCreateChannelAsync(CancellationToken cancellationToken)
    {
        // Reutiliza a conexão e o channel já existentes.
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
        {
            return _channel;
        }

        // Remove recursos fechados ou inválidos antes
        // de criar uma nova conexão.
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

        IChannel channel = await _connection.CreateChannelAsync(
            options: channelOptions,
            cancellationToken);

        // Armazena o channel antes da declaração para garantir
        // que ele seja descartado caso a declaração falhe.
        _channel = channel;

        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        return channel;
    }

    /// <summary>
    /// Fecha e libera o channel e a conexão com o RabbitMQ.
    /// </summary>
    private async Task DisposeRabbitMqResourcesAsync()
    {
        // Retira primeiro a referência do campo para impedir
        // que um recurso em processo de descarte seja reutilizado.
        IChannel? channel = _channel;
        _channel = null;

        if (channel is not null)
        {
            try
            {
                if (channel.IsOpen)
                {
                    await channel.CloseAsync();
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "An error occurred while closing the RabbitMQ channel.");
            }

            try
            {
                await channel.DisposeAsync();
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "An error occurred while disposing the RabbitMQ channel.");
            }
        }

        IConnection? connection = _connection;
        _connection = null;

        if (connection is not null)
        {
            try
            {
                if (connection.IsOpen)
                {
                    await connection.CloseAsync();
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "An error occurred while closing the RabbitMQ connection.");
            }

            try
            {
                await connection.DisposeAsync();
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "An error occurred while disposing the RabbitMQ connection.");
            }
        }
    }

    /// <summary>
    /// Libera os recursos utilizados pelo publisher
    /// durante o encerramento da aplicação.
    /// </summary>
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