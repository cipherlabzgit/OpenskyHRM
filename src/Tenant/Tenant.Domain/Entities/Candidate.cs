namespace Tenant.Domain.Entities;

public class Candidate
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    public string? Website { get; set; }
    
    // Current Employment
    public string? CurrentCompany { get; set; }
    public string? CurrentTitle { get; set; }
    public decimal? CurrentSalary { get; set; }
    public string? NoticePeriod { get; set; }
    public decimal? ExpectedSalary { get; set; }
    
    // Source Tracking
    public string? Source { get; set; } // LinkedIn, Indeed, Referral, Career Portal, etc.
    public Guid? ReferredByEmployeeId { get; set; }
    public Employee? ReferredByEmployee { get; set; }
    public string? ReferralCode { get; set; }
    
    // Tags and Notes
    public string? Tags { get; set; } // Comma-separated
    public string? Notes { get; set; }
    public int? Rating { get; set; } // 1-5 stars
    
    // Duplicate Detection
    public string? EmailHash { get; set; }
    public string? PhoneHash { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    
    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public ICollection<CandidateDocument> Documents { get; set; } = new List<CandidateDocument>();
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
}
