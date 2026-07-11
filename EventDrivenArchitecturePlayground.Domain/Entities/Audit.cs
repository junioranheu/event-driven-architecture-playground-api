namespace EventDrivenArchitecturePlayground.Domain.Entities;

public abstract class Audit
{
    public Guid Id { get; set; }

    public DateTime? CreatedDate { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? LastModificationDate { get; set; }

    public Guid? LastModificationBy { get; set; }

    public bool Status { get; set; }
}