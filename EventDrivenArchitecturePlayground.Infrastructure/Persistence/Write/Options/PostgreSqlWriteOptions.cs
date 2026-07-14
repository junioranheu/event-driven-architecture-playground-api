namespace EventDrivenArchitecturePlayground.Infrastructure.Persistence.Write.Options;

/// <summary>
/// Configurações do PostgreSQL utilizado para escrita
/// e armazenamento das mensagens do Outbox.
/// </summary>
public sealed class PostgreSqlWriteOptions
{
    public const string SectionName = "PostgreSqlWrite";
    public string ConnectionString { get; init; } = string.Empty;
}