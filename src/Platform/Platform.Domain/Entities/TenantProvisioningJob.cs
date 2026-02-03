namespace Platform.Domain.Entities;

public class TenantProvisioningJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public ProvisioningStatus Status { get; set; } = ProvisioningStatus.Pending;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? LastError { get; set; }
}

public enum ProvisioningStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3
}
