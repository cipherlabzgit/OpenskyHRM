namespace Tenant.Domain.Entities;

public class ShiftTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? BreakDuration { get; set; }
    public decimal WorkingHours { get; set; }
    public bool IsNightShift { get; set; }
    public bool IsFlexible { get; set; }
    public int? GracePeriodMinutes { get; set; }
    public int? EarlyCheckInMinutes { get; set; }
    public string? Color { get; set; } // For calendar display
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
