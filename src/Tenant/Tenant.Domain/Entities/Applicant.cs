namespace Tenant.Domain.Entities;

public class Applicant
{
    public Guid Id { get; set; }
    public Guid JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ResumePath { get; set; }
    public string? CoverLetterPath { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    public string? Source { get; set; } // LinkedIn, Indeed, Referral, etc.
    public string? ReferredBy { get; set; }
    public ApplicantStatus Status { get; set; } = ApplicantStatus.New;
    public ApplicantStage Stage { get; set; } = ApplicantStage.Applied;
    public int? Rating { get; set; }
    public string? Notes { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public string? NoticePeriod { get; set; }
    public DateTime AppliedAt { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
}

public enum ApplicantStatus
{
    New = 0,
    InReview = 1,
    Shortlisted = 2,
    Interviewing = 3,
    OfferExtended = 4,
    Hired = 5,
    Rejected = 6,
    Withdrawn = 7
}

public enum ApplicantStage
{
    Applied = 0,
    Screening = 1,
    PhoneInterview = 2,
    TechnicalInterview = 3,
    ManagerInterview = 4,
    FinalInterview = 5,
    BackgroundCheck = 6,
    Offer = 7,
    Onboarding = 8
}
