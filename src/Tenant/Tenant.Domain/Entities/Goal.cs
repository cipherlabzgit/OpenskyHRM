namespace Tenant.Domain.Entities;

public class Goal
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GoalCategory Category { get; set; } = GoalCategory.Individual;
    public GoalPriority Priority { get; set; } = GoalPriority.Medium;
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.NotStarted;
    public int ProgressPercent { get; set; }
    public string? Metrics { get; set; }
    public string? Notes { get; set; }
    public Guid? ParentGoalId { get; set; }
    public Goal? ParentGoal { get; set; }
    public decimal? Weight { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum GoalCategory
{
    Individual = 0,
    Team = 1,
    Department = 2,
    Company = 3
}

public enum GoalPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum GoalStatus
{
    NotStarted = 0,
    InProgress = 1,
    OnHold = 2,
    Completed = 3,
    Cancelled = 4
}
