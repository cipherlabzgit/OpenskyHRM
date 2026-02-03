namespace Tenant.Domain.Entities;

public class LeaveApproval
{
    public Guid Id { get; set; }
    public Guid LeaveRequestId { get; set; }
    public LeaveRequest LeaveRequest { get; set; } = null!;
    public Guid ApproverId { get; set; }
    public int ApprovalLevel { get; set; } = 1;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public string? Comments { get; set; }
    public DateTime? ActionDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Escalated = 3
}
