namespace EventDrivenArchitecturePlayground.Infrastructure.Messaging.RabbitMq;

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
}