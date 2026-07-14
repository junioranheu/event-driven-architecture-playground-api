namespace EventDrivenArchitecturePlayground.Infrastructure.Persistence.Read.Options;

/// <summary>
/// Configurações do PostgreSQL utilizado exclusivamente
/// para consultas do lado de leitura do CQRS.
/// </summary>
public sealed class PostgreSqlReadOptions
{
    public const string SectionName = "PostgreSqlRead";
    public string ConnectionString { get; set; } = string.Empty;
}