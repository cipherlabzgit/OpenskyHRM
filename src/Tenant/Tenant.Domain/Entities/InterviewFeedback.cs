namespace Tenant.Domain.Entities;

public class InterviewFeedback
{
    public Guid Id { get; set; }
    public Guid InterviewId { get; set; }
    public Interview Interview { get; set; } = null!;
    public Guid InterviewerId { get; set; }
    public User? Interviewer { get; set; }
    
    // Scoring
    public int? OverallRating { get; set; } // 1-10
    public string? TechnicalScore { get; set; } // JSON with category scores
    public string? CommunicationScore { get; set; }
    public string? CulturalFitScore { get; set; }
    public string? ProblemSolvingScore { get; set; }
    
    // Feedback
    public string? Strengths { get; set; }
    public string? Weaknesses { get; set; }
    public string? OverallComments { get; set; }
    public Recommendation Recommendation { get; set; } = Recommendation.Pending;
    public string? RecommendationNotes { get; set; }
    
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum Recommendation
{
    Pending = 0,
    StrongYes = 1,
    Yes = 2,
    Maybe = 3,
    No = 4,
    StrongNo = 5
}
