namespace Tenant.Domain.Entities;

public class JobPosting
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? JobCode { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string? Location { get; set; }
    public string EmploymentType { get; set; } = "FullTime";
    public string? ExperienceLevel { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public string? Responsibilities { get; set; }
    public string? Benefits { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? Currency { get; set; }
    public bool ShowSalary { get; set; }
    public int? Openings { get; set; }
    public JobPostingStatus Status { get; set; } = JobPostingStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ClosingDate { get; set; }
    public Guid? HiringManagerId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Applicant> Applicants { get; set; } = new List<Applicant>();
}

public enum JobPostingStatus
{
    Draft = 0,
    Published = 1,
    OnHold = 2,
    Closed = 3,
    Filled = 4
}
