namespace Tenant.Domain.Entities;

public class AttendanceLog
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public AttendanceLogType LogType { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? Location { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? IpAddress { get; set; }
    public string? Notes { get; set; }
    public bool IsManualEntry { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public enum AttendanceLogType
{
    CheckIn = 0,
    CheckOut = 1,
    BreakStart = 2,
    BreakEnd = 3
}
