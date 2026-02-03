namespace Tenant.Domain.Entities;

public class JobInfo
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string JobTitle { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? DesignationId { get; set; }
    public Designation? Designation { get; set; }
    public Guid? ReportsToId { get; set; }
    public Employee? ReportsTo { get; set; }
    public string? EmploymentType { get; set; } // FullTime, PartTime, Contract, Intern
    public string? WorkLocation { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
