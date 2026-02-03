namespace Tenant.Domain.Entities;

public class BenefitPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public BenefitType Type { get; set; }
    public string? Description { get; set; }
    public string? Provider { get; set; }
    public decimal? EmployerContribution { get; set; }
    public decimal? EmployeeContribution { get; set; }
    public string? ContributionType { get; set; } // Fixed, Percentage
    public string? EligibilityCriteria { get; set; }
    public int? WaitingPeriodDays { get; set; }
    public DateTime? EnrollmentStartDate { get; set; }
    public DateTime? EnrollmentEndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<EmployeeBenefit> EmployeeBenefits { get; set; } = new List<EmployeeBenefit>();
}

public enum BenefitType
{
    Health = 0,
    Dental = 1,
    Vision = 2,
    Life = 3,
    Disability = 4,
    Retirement = 5,
    PTO = 6,
    Wellness = 7,
    Education = 8,
    Other = 9
}
