namespace Tenant.Domain.Entities;

public class OnboardingTemplateTask
{
    public Guid Id { get; set; }
    public Guid OnboardingTemplateId { get; set; }
    public OnboardingTemplate OnboardingTemplate { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; } // Documentation, Training, Equipment, Access, etc.
    public int SortOrder { get; set; }
    public int? DueDaysFromStart { get; set; }
    public string? AssigneeRole { get; set; } // HR, Manager, IT, Employee
    public bool IsRequired { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
