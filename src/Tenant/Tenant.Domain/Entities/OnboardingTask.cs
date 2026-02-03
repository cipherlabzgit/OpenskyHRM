namespace Tenant.Domain.Entities;

public class OnboardingTask
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid? TemplateTaskId { get; set; }
    public OnboardingTemplateTask? TemplateTask { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int SortOrder { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? AssignedToId { get; set; }
    public OnboardingTaskStatus Status { get; set; } = OnboardingTaskStatus.Pending;
    public DateTime? CompletedDate { get; set; }
    public Guid? CompletedById { get; set; }
    public string? Notes { get; set; }
    public bool IsRequired { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum OnboardingTaskStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Skipped = 3,
    Blocked = 4
}
