namespace Platform.Domain.Entities;

public class TenantSchemaState
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string LastAppliedMigration { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}
