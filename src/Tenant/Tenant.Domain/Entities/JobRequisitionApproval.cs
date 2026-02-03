namespace Tenant.Domain.Entities;

public class JobRequisitionApproval
{
    public Guid Id { get; set; }
    public Guid RequisitionId { get; set; }
    public JobRequisition Requisition { get; set; } = null!;
    public Guid ApproverId { get; set; }
    public User? Approver { get; set; }
    public int ApprovalLevel { get; set; }
    public RequisitionApprovalStatus Status { get; set; } = RequisitionApprovalStatus.Pending;
    public string? Comments { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public enum RequisitionApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
