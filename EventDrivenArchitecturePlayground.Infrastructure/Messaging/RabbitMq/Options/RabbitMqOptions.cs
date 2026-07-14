namespace EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq.Options;

/// <summary>
/// Representa as configurações utilizadas pelo RabbitMQ.
/// </summary>
public sealed class RabbitMqOptions
{
    /// <summary>
    /// Nome da seção utilizada nos arquivos de configuração.
    /// </summary>
    public const string SectionName = "RabbitMQ";

    /// <summary>
    /// Obtém a URL completa de conexão com o RabbitMQ.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// Obtém o nome do exchange utilizado para publicar os eventos.
    /// </summary>
    public string ExchangeName { get; init; } = string.Empty;

    /// <summary>
    /// Obtém o nome da fila (queue) utilizada para receber as mensagens.
    /// </summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// Obtém a chave de roteamento (binding key) utilizada para vincular a fila ao exchange.
    /// </summary>
    public string BindingKey { get; set; } = string.Empty;
}