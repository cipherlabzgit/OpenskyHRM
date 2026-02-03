namespace Tenant.Domain.Entities;

public class Offer
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    public Guid? RequisitionId { get; set; }
    public JobRequisition? Requisition { get; set; }
    
    // Offer Details
    public string OfferNumber { get; set; } = string.Empty;
    public Guid? DesignationId { get; set; }
    public Designation? Designation { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    // Compensation
    public decimal BaseSalary { get; set; }
    public string Currency { get; set; } = "USD";
    public string? SalaryBreakdown { get; set; } // JSON string
    public string? Benefits { get; set; } // JSON string
    
    // Dates
    public DateTime? JoiningDate { get; set; }
    public DateTime? OfferDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    
    // Status
    public OfferStatus Status { get; set; } = OfferStatus.Draft;
    public string? OfferLetterTemplate { get; set; }
    public string? OfferLetterContent { get; set; }
    public string? OfferDocumentPath { get; set; }
    public string? SignedDocumentPath { get; set; }
    
    // Approval
    public Guid? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    // Acceptance
    public DateTime? SentAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? AcceptanceNotes { get; set; }
    
    // Conversion
    public Guid? ConvertedToEmployeeId { get; set; }
    public Employee? ConvertedToEmployee { get; set; }
    public DateTime? ConvertedAt { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum OfferStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Sent = 3,
    Accepted = 4,
    Rejected = 5,
    Expired = 6,
    Withdrawn = 7,
    Converted = 8
}
