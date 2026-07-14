using EventDrivenArchitecturePlayground.Application.Abstractions.Persistence;
using EventDrivenArchitecturePlayground.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Security.Claims;
using static EventDrivenArchitecturePlayground.Utils.Fixtures.Get;

namespace EventDrivenArchitecturePlayground.Infrastructure.Persistence.Write;

/// <summary>
/// Representa o contexto de escrita da aplicação,
/// responsável pelas operações de persistência de dados
/// no banco de dados seguindo o padrão CQRS.
/// </summary>
public sealed class ExpensesWriteDbContext(DbContextOptions<ExpensesWriteDbContext> options, IHttpContextAccessor httpContextAccessor) : DbContext(options), IUnitOfWork
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<Expense> Expenses => Set<Expense>();

    #region extras
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region delete_behavior
        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Cascade;
        }
        #endregion

        #region postgreSQL_datetime_normalize_utc
        var utcConverter = new ValueConverter<DateTime, DateTime>(
           x => x.Kind == DateTimeKind.Utc ? x : x.ToUniversalTime(), // Salva como UTC;
           x => DateTime.SpecifyKind(x, DateTimeKind.Utc)             // Lê como UTC;
        );

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(
                        new ValueConverter<DateTime?, DateTime?>(
                            x => x.HasValue ? (x.Value.Kind == DateTimeKind.Utc ? x.Value : x.Value.ToUniversalTime()) : x,
                            x => x.HasValue ? DateTime.SpecifyKind(x.Value, DateTimeKind.Utc) : x
                        )
                    );
                }
            }
        }
        #endregion

        base.OnModelCreating(modelBuilder);
    }

    private Guid UserIdAuth
    {
        get
        {
            ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated ?? false)
            {
                string? userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (Guid.TryParse(userIdClaim, out Guid userIdAuth))
                {
                    return userIdAuth;
                }
            }

            return Guid.Empty;
        }
    }

    private void ApplyAuditLogRules()
    {
        foreach (var entry in ChangeTracker.Entries<Audit>())
        {
            if (entry.Entity is Audit audit)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        if (audit.CreatedDate is null)
                        {
                            audit.CreatedDate = GetDate();
                            audit.CreatedBy = UserIdAuth;
                            audit.Status = true;
                        }

                        break;

                    case EntityState.Modified:
                        audit.LastModificationDate = GetDate();
                        audit.LastModificationBy = UserIdAuth;

                        break;
                }
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditLogRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditLogRules();
        return base.SaveChanges();
    }
    #endregion
}