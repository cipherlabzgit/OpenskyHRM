namespace Tenant.Domain.Entities;

public class OffboardingTask
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; } // Equipment Return, Access Revoke, Knowledge Transfer, Exit Interview
    public int SortOrder { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? AssignedToId { get; set; }
    public OffboardingTaskStatus Status { get; set; } = OffboardingTaskStatus.Pending;
    public DateTime? CompletedDate { get; set; }
    public Guid? CompletedById { get; set; }
    public string? Notes { get; set; }
    public bool IsRequired { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum OffboardingTaskStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Skipped = 3,
    NotApplicable = 4
}
