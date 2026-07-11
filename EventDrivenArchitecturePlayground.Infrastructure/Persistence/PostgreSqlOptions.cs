namespace EventDrivenArchitecturePlayground.Infrastructure.Persistence;

/// <summary>
/// Representa as configurações utilizadas para conexão com o PostgreSQL.
/// </summary>
public sealed class PostgreSqlOptions
{
    /// <summary>
    /// Nome da seção utilizada nos arquivos de configuração.
    /// </summary>
    public const string SectionName = "PostgreSql";

    /// <summary>
    /// Obtém a string de conexão com o PostgreSQL.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;
}