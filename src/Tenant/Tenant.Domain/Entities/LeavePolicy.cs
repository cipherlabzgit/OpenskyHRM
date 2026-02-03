namespace Tenant.Domain.Entities;

public class LeavePolicy
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;
    public decimal AnnualAllocation { get; set; }
    public AccrualMethod AccrualMethod { get; set; } = AccrualMethod.Yearly;
    public decimal? MaxCarryForward { get; set; }
    public decimal? MaxAccumulation { get; set; }
    public int? MinServiceDaysRequired { get; set; }
    public int? MinNoticeDays { get; set; }
    public int? MaxConsecutiveDays { get; set; }
    public bool AllowNegativeBalance { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public bool RequiresDocument { get; set; }
    public int? DocumentRequiredAfterDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum AccrualMethod
{
    Yearly = 0,
    Monthly = 1,
    BiWeekly = 2,
    Weekly = 3,
    Daily = 4
}
