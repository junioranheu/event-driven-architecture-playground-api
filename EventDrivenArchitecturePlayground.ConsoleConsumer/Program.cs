using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

Console.Title = "Consumer (TEST)";

IConfiguration configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

string rabbitMqUrl = configuration["RabbitMQ:Url"] ??
    throw new InvalidOperationException("RabbitMQ:Url was not configured in User Secrets.");

const string exchangeName = "expenses.events";

// Fila exclusiva deste Console App.
// Não utilize o mesmo nome da fila consumida pela API.
const string queueName = "event-driven-playground.console";

// (!) Mesma key utilizada pelo publisher da API em ExpenseCreatedIntegrationEvent.
const string bindingKey = "expenses.expense-created.v1";

ConnectionFactory connectionFactory = new()
{
    Uri = new Uri(rabbitMqUrl),
    AutomaticRecoveryEnabled = true,
    TopologyRecoveryEnabled = true,
    ClientProvidedName = "event-driven-playground:console-consumer"
};

await using IConnection connection = await connectionFactory.CreateConnectionAsync();

await using IChannel channel = await connection.CreateChannelAsync();

// Garante que o mesmo exchange usado pelo publisher exista.
await channel.ExchangeDeclareAsync(
    exchange: exchangeName,
    type: ExchangeType.Topic,
    durable: true,
    autoDelete: false,
    arguments: null);

// Cria uma fila exclusiva para este Console App.
await channel.QueueDeclareAsync(
    queue: queueName,
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);

// Faz esta fila receber os eventos ExpenseCreated.
await channel.QueueBindAsync(
    queue: queueName,
    exchange: exchangeName,
    routingKey: bindingKey,
    arguments: null);

// Limita a quantidade de mensagens ainda não confirmadas.
await channel.BasicQosAsync(
    prefetchSize: 0,
    prefetchCount: 10,
    global: false);

AsyncEventingBasicConsumer consumer = new(channel);

consumer.ReceivedAsync += async (_, eventArgs) =>
{
    string content = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

    try
    {
        Console.WriteLine();
        Console.WriteLine("Mensagem recebida:");
        Console.WriteLine($"MessageId: {eventArgs.BasicProperties.MessageId}");
        Console.WriteLine($"EventType: {eventArgs.BasicProperties.Type}");
        Console.WriteLine($"RoutingKey: {eventArgs.RoutingKey}");
        Console.WriteLine($"Content: {content}");

        // Confirma que a mensagem foi processada.
        await channel.BasicAckAsync(
            deliveryTag: eventArgs.DeliveryTag,
            multiple: false);
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Erro ao processar a mensagem: {exception.Message}");

        // Devolve a mensagem para a fila.
        await channel.BasicNackAsync(
            deliveryTag: eventArgs.DeliveryTag,
            multiple: false,
            requeue: true);
    }
};

string consumerTag = await channel.BasicConsumeAsync(
    queue: queueName,
    autoAck: false,
    consumer: consumer);

Console.WriteLine("Console Consumer iniciado.");
Console.WriteLine($"Queue: {queueName}");
Console.WriteLine($"Binding: {bindingKey}");
Console.WriteLine("Pressione ENTER para encerrar.");

Console.ReadLine();

await channel.BasicCancelAsync(consumerTag);