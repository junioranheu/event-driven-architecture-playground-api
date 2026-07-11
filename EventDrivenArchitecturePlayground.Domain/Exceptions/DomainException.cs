namespace EventDrivenArchitecturePlayground.Domain.Exceptions;

/// <summary>
/// Representa um erro causado pela violação de uma regra de negócio
/// pertencente ao domínio da aplicação.
/// </summary>
/// <remarks>
/// Inicializa uma nova instância de <see cref="DomainException"/>.
/// </remarks>
/// <param name="message">
/// Mensagem que descreve a regra de negócio violada.
/// </param>
public sealed class DomainException(string message) : Exception(message)
{
}