using System.ComponentModel;
using System.Reflection;

namespace EventDrivenArchitecturePlayground.Utils.Fixtures;

public static partial class Get
{
    /// <summary>
    /// Obtém o horário atual, forçando ao horário de Brasilia;
    /// </summary>
    public static DateTime GetDate()
    {
        return DateTime.UtcNow;
    }

    /// <summary>
    /// Converte um DateTime qualquer para o formato UTC, tratando automaticamente o DateTimeKind.
    /// Se o DateTime estiver em Local, será convertido usando o fuso horário local.
    /// Se estiver em Unspecified, assumirá como horário local antes de converter.
    /// </summary>
    /// <param name="date">A data a ser convertida.</param>
    /// <returns>Um DateTime representando a mesma data/hora em UTC.</returns>
    public static DateTime ConvertToUtc(DateTime date)
    {
        if (date.Kind == DateTimeKind.Utc)
        {
            return date;
        }

        if (date.Kind == DateTimeKind.Unspecified)
        {
            date = DateTime.SpecifyKind(date, DateTimeKind.Local);
        }

        return date.ToUniversalTime();
    }

    /// <summary>
    /// Converte um DateTime qualquer para o horário de Brasília (UTC-3 ou horário de verão, quando aplicável),
    /// tratando automaticamente o DateTimeKind.
    /// Se o DateTime estiver em Local, será convertido usando o fuso horário local.
    /// Se estiver em Unspecified, assumirá como horário local antes de converter.
    /// </summary>
    /// <param name="date">A data a ser convertida.</param>
    /// <returns>Um DateTime representando a mesma data/hora no horário de Brasília.</returns>
    public static DateTime ConvertToBrasiliaTime(DateTime date)
    {
        if (date.Kind == DateTimeKind.Unspecified)
        {
            date = DateTime.SpecifyKind(date, DateTimeKind.Local);
        }

        // Converte para UTC primeiro, caso necessário
        DateTime utcDate = date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime();

        TimeZoneInfo brasiliaTimeZone;

        try
        {
            // Windows
            brasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            // Linux/Mac;
            brasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utcDate, brasiliaTimeZone);
    }

    /// <summary>
    /// Obtém a descrição de um enum;
    /// </summary>
    public static string GetEnumDesc(Enum enumVal)
    {
        MemberInfo[] memInfo = enumVal.GetType().GetMember(enumVal.ToString());
        DescriptionAttribute? attribute = CustomAttributeExtensions.GetCustomAttribute<DescriptionAttribute>(memInfo[0]);

        string? description = attribute?.Description;

        if (string.IsNullOrEmpty(description))
        {
            return enumVal.ToString();
        }

        return description;
    }
}