namespace Tenant.Domain.Entities;

public class EmployeeRoster
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid ShiftTemplateId { get; set; }
    public ShiftTemplate ShiftTemplate { get; set; } = null!;
    public DateTime Date { get; set; }
    public TimeSpan? CustomStartTime { get; set; }
    public TimeSpan? CustomEndTime { get; set; }
    public RosterStatus Status { get; set; } = RosterStatus.Scheduled;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum RosterStatus
{
    Scheduled = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3,
    SwapRequested = 4
}
