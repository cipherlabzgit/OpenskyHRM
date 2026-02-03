namespace Tenant.Domain.Entities;

public class PerformanceReview
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid? ReviewerId { get; set; }
    public Employee? Reviewer { get; set; }
    public string ReviewPeriod { get; set; } = string.Empty; // Q1 2024, H1 2024, 2024
    public DateTime ReviewDate { get; set; }
    public DateTime? DueDate { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Draft;
    public decimal? OverallRating { get; set; }
    public string? EmployeeSelfReview { get; set; }
    public string? ManagerReview { get; set; }
    public string? Strengths { get; set; }
    public string? AreasForImprovement { get; set; }
    public string? Goals { get; set; }
    public string? DevelopmentPlan { get; set; }
    public string? EmployeeComments { get; set; }
    public DateTime? EmployeeAcknowledgedAt { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum ReviewStatus
{
    Draft = 0,
    SelfReviewPending = 1,
    ManagerReviewPending = 2,
    Completed = 3,
    Acknowledged = 4
}
