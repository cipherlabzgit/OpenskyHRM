namespace Tenant.Domain.Entities;

public class EmployeeHistory
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string ChangeType { get; set; } = string.Empty; // Promotion, Transfer, Salary, Department, etc.
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public Guid? ChangedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
