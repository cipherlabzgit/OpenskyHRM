namespace Tenant.Domain.Entities;

public class Holiday
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public HolidayType Type { get; set; } = HolidayType.Public;
    public bool IsRecurring { get; set; }
    public string? Description { get; set; }
    public string? ApplicableBranches { get; set; } // Comma-separated branch IDs or "All"
    public string? ApplicableDepartments { get; set; } // Comma-separated dept IDs or "All"
    public bool IsOptional { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum HolidayType
{
    Public = 0,
    Religious = 1,
    National = 2,
    CompanySpecific = 3,
    Optional = 4
}
