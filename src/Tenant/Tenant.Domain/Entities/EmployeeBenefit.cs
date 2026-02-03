namespace Tenant.Domain.Entities;

public class EmployeeBenefit
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid BenefitPlanId { get; set; }
    public BenefitPlan BenefitPlan { get; set; } = null!;
    public DateTime EnrollmentDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? CoverageLevel { get; set; } // Employee, Employee+Spouse, Family
    public decimal? EmployeeContribution { get; set; }
    public decimal? EmployerContribution { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum EnrollmentStatus
{
    Pending = 0,
    Active = 1,
    Waived = 2,
    Terminated = 3
}
