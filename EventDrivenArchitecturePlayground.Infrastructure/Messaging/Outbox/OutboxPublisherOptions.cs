namespace EventDrivenArchitecturePlayground.Infrastructure.Messaging.Outbox;

/// <summary>
/// Representa as configurações utilizadas pelo processador do Outbox.
/// </summary>
public sealed class OutboxPublisherOptions
{
    public const string SectionName = "OutboxPublisher";

    // Intervalo entre verificações no banco.
    public int PollingIntervalSeconds { get; init; } = 5;

    // Quantidade máxima processada por vez.
    public int BatchSize { get; init; } = 20;

    // Limite máximo do intervalo entre tentativas.
    public int MaxRetryDelaySeconds { get; init; } = 300;
}