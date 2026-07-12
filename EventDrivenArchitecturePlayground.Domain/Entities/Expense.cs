using EventDrivenArchitecturePlayground.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventDrivenArchitecturePlayground.Domain.Entities;

/// <summary>
/// Representa uma despesa registrada pelo usuário.
/// </summary>
[Table("expenses")]
[Index(nameof(OccurredAt), Name = "ix_expenses_occurred_at")]
public sealed class Expense : Audit
{
    /// <summary>
    /// Quantidade máxima de caracteres permitida para o nome do item.
    /// </summary>
    private const int MaxItemLength = 150;

    /// <summary>
    /// Inicializa uma nova instância válida de <see cref="Expense"/>.
    /// </summary>
    /// <param name="id">Identificador único da despesa.</param>
    /// <param name="item">Nome ou descrição do item da despesa.</param>
    /// <param name="amount">Valor monetário da despesa.</param>
    /// <param name="occurredAt">
    /// Data e hora em que a despesa ocorreu.
    /// </param>
    private Expense(
        Guid id,
        string item,
        decimal amount,
        DateTime occurredAt)
    {
        Id = id;
        Item = item;
        Amount = amount;
        OccurredAt = occurredAt;
    }

    [Required]
    [MaxLength(MaxItemLength)]
    [Column("item")]
    public string Item { get; private set; } = string.Empty;

    [Required]
    [Column("amount", TypeName = "numeric(18,2)")]
    public decimal Amount { get; private set; }

    /// <summary>
    /// Obtém a data e hora em que a despesa ocorreu,
    /// incluindo a informação de deslocamento do fuso horário.
    /// </summary>
    ///     [Required]
    [Column("occurred_at")]
    public DateTime OccurredAt { get; private set; }

    /// <summary>
    /// Cria uma nova despesa aplicando todas as regras de negócio necessárias.
    /// </summary>
    /// <param name="item">Nome ou descrição do item da despesa.</param>
    /// <param name="amount">Valor monetário da despesa.</param>
    /// <param name="occurredAt">Data e hora em que a despesa ocorreu.</param>
    /// <returns>Uma nova instância válida de <see cref="Expense"/>.</returns>
    /// <exception cref="DomainException">
    /// Lançada quando o item ou o valor informado viola uma regra de negócio.
    /// </exception>
    public static Expense Create(
        string item,
        decimal amount,
        DateTime occurredAt)
    {
        string normalizedItem = item?.Trim() ?? string.Empty;

        ValidateItem(normalizedItem);
        ValidateAmount(amount);

        return new Expense(
            Guid.NewGuid(),
            normalizedItem,
            decimal.Round(amount, 2, MidpointRounding.ToEven),
            occurredAt);
    }

    /// <summary>
    /// Valida o nome ou a descrição do item da despesa.
    /// </summary>
    /// <param name="item">Item que será validado.</param>
    /// <exception cref="DomainException">
    /// Lançada quando o item está vazio ou ultrapassa o tamanho máximo permitido.
    /// </exception>
    private static void ValidateItem(string item)
    {
        if (string.IsNullOrWhiteSpace(item))
        {
            throw new DomainException("The expense item is required.");
        }

        if (item.Length > MaxItemLength)
        {
            throw new DomainException($"The expense item must contain at most {MaxItemLength} characters.");
        }
    }

    /// <summary>
    /// Valida o valor monetário da despesa.
    /// </summary>
    /// <param name="amount">Valor que será validado.</param>
    /// <exception cref="DomainException">
    /// Lançada quando o valor é menor ou igual a zero.
    /// </exception>
    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("The expense amount must be greater than zero.");
        }
    }
}