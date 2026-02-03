namespace Tenant.Domain.Entities;

public class Application
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Candidate Candidate { get; set; } = null!;
    public Guid RequisitionId { get; set; }
    public JobRequisition Requisition { get; set; } = null!;
    
    // Application Details
    public ApplicationStage Stage { get; set; } = ApplicationStage.Applied;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.New;
    public string? CoverLetter { get; set; }
    public string? ScreeningAnswers { get; set; } // JSON string
    
    // Timeline
    public DateTime AppliedAt { get; set; }
    public DateTime? ShortlistedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    
    // Tracking
    public string? Source { get; set; }
    public string? ReferralCode { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    
    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
    public ICollection<ApplicationActivity> Activities { get; set; } = new List<ApplicationActivity>();
}

public enum ApplicationStage
{
    Applied = 0,
    Screening = 1,
    Shortlisted = 2,
    Interview = 3,
    Assessment = 4,
    Offered = 5,
    Hired = 6,
    Rejected = 7,
    Withdrawn = 8
}

public enum ApplicationStatus
{
    New = 0,
    InReview = 1,
    Shortlisted = 2,
    Interviewing = 3,
    AssessmentPending = 4,
    OfferExtended = 5,
    OfferAccepted = 6,
    OfferRejected = 7,
    Hired = 8,
    Rejected = 9,
    Withdrawn = 10,
    OnHold = 11
}
