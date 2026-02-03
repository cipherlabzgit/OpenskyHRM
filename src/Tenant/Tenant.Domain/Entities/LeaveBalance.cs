namespace Tenant.Domain.Entities;

public class LeaveBalance
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;
    public int Year { get; set; }
    public decimal Entitled { get; set; }
    public decimal Used { get; set; }
    public decimal Pending { get; set; }
    public decimal CarriedForward { get; set; }
    public decimal Adjustment { get; set; }
    public decimal Balance => Entitled + CarriedForward + Adjustment - Used - Pending;
    public DateTime? LastAccrualDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
