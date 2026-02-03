namespace Tenant.Domain.Entities;

public class JobRequisition
{
    public Guid Id { get; set; }
    public string RequisitionNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? DesignationId { get; set; }
    public Designation? Designation { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string? Location { get; set; }
    public string EmploymentType { get; set; } = "FullTime"; // FullTime, PartTime, Contract
    public int? Openings { get; set; } = 1;
    
    // Budget
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? Currency { get; set; } = "USD";
    
    // Requirements
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public string? Responsibilities { get; set; }
    public string? RequiredSkills { get; set; }
    public string? PreferredSkills { get; set; }
    public int? MinExperienceYears { get; set; }
    public int? MaxExperienceYears { get; set; }
    public string? EducationLevel { get; set; }
    
    // Workflow
    public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;
    public Guid? RequestedById { get; set; }
    public User? RequestedBy { get; set; }
    public Guid? HiringManagerId { get; set; }
    public Employee? HiringManager { get; set; }
    
    // Dates
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? TargetStartDate { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    
    public ICollection<JobRequisitionApproval> Approvals { get; set; } = new List<JobRequisitionApproval>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}

public enum RequisitionStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Published = 3,
    OnHold = 4,
    Closed = 5,
    Cancelled = 6
}
