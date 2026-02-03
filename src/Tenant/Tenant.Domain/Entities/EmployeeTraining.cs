namespace Tenant.Domain.Entities;

public class EmployeeTraining
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid TrainingId { get; set; }
    public Training Training { get; set; } = null!;
    public DateTime? AssignedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public TrainingStatus Status { get; set; } = TrainingStatus.Assigned;
    public int? ProgressPercent { get; set; }
    public decimal? Score { get; set; }
    public bool? Passed { get; set; }
    public string? CertificateNumber { get; set; }
    public DateTime? CertificateExpiryDate { get; set; }
    public string? Feedback { get; set; }
    public int? Rating { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum TrainingStatus
{
    Assigned = 0,
    NotStarted = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Expired = 5,
    Waived = 6
}
