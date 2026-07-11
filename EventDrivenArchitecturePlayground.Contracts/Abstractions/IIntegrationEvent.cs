namespace EventDrivenArchitecturePlayground.Contracts.Abstractions;

/// <summary>
/// Define as informações básicas que todo evento de integração deve possuir.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOnUtc { get; }
}