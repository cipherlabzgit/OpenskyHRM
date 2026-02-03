namespace Tenant.Domain.Entities;

public class Compensation
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public decimal BaseSalary { get; set; }
    public string Currency { get; set; } = "USD";
    public string PayFrequency { get; set; } = "Monthly"; // Monthly, BiWeekly, Weekly
    public decimal? HousingAllowance { get; set; }
    public decimal? TransportAllowance { get; set; }
    public decimal? MealAllowance { get; set; }
    public decimal? OtherAllowances { get; set; }
    public decimal? Bonus { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
