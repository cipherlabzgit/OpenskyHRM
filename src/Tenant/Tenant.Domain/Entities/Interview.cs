namespace Tenant.Domain.Entities;

public class Interview
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    public string InterviewType { get; set; } = string.Empty; // Phone, Video, InPerson, Technical, HR, Panel
    public string InterviewRound { get; set; } = string.Empty; // Round 1, Round 2, Final, etc.
    public DateTime ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string? Location { get; set; }
    public string? MeetingLink { get; set; }
    public string? MeetingId { get; set; }
    public string? MeetingPassword { get; set; }
    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;
    public string? InterviewerIds { get; set; } // JSON array of GUIDs
    public string? PanelMembers { get; set; } // JSON array
    public string? Agenda { get; set; }
    public string? Notes { get; set; }
    public int? OverallRating { get; set; }
    public string? Feedback { get; set; }
    public string? Strengths { get; set; }
    public string? Weaknesses { get; set; }
    public string? Recommendation { get; set; }
    public Guid? ScheduledById { get; set; }
    public User? ScheduledBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    
    public ICollection<InterviewFeedback> Feedbacks { get; set; } = new List<InterviewFeedback>();
}

public enum InterviewStatus
{
    Scheduled = 0,
    Confirmed = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    NoShow = 5,
    Rescheduled = 6
}
