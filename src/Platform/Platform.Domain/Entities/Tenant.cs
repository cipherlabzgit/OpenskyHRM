namespace Platform.Domain.Entities;

public class Tenant
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? TimeZone { get; set; }
    public string? Currency { get; set; }
    public string? Plan { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Provisioning;
    public DateTime? TrialEndUtc { get; set; }
    
    // Admin info (for email-based tenant lookup)
    public string? AdminEmail { get; set; }
    
    // Database connection info
    public string DbName { get; set; } = string.Empty;
    public string? DbHost { get; set; }
    public int? DbPort { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    // Navigation properties
    public ICollection<TenantProvisioningJob> ProvisioningJobs { get; set; } = new List<TenantProvisioningJob>();
    public ICollection<TenantSchemaState> SchemaStates { get; set; } = new List<TenantSchemaState>();
    public ICollection<BillingEventOutbox> BillingEvents { get; set; } = new List<BillingEventOutbox>();
}

public enum TenantStatus
{
    Provisioning = 0,
    Active = 1,
    Suspended = 2,
    Deleted = 3
}
