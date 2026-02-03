namespace Tenant.Domain.Entities;

public class Assessment
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Candidate Candidate { get; set; } = null!;
    public Guid? ApplicationId { get; set; }
    public Application? Application { get; set; }
    public string AssessmentName { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = string.Empty; // Technical, Behavioral, Aptitude, etc.
    public string? Instructions { get; set; }
    public string? Questions { get; set; } // JSON string
    public string? Answers { get; set; } // JSON string
    public decimal? Score { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? PassingScore { get; set; }
    public AssessmentStatus Status { get; set; } = AssessmentStatus.Pending;
    public bool IsPassed { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Feedback { get; set; }
    public string? Attachments { get; set; } // JSON array of file paths
    public Guid? AssignedById { get; set; }
    public User? AssignedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum AssessmentStatus
{
    Pending = 0,
    Assigned = 1,
    InProgress = 2,
    Completed = 3,
    Evaluated = 4,
    Expired = 5
}
