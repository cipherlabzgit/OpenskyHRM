namespace Tenant.Domain.Entities;

public class Training
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public TrainingType Type { get; set; } = TrainingType.Online;
    public string? Category { get; set; }
    public string? Provider { get; set; }
    public string? Instructor { get; set; }
    public int? DurationHours { get; set; }
    public decimal? Cost { get; set; }
    public string? Currency { get; set; }
    public string? Location { get; set; }
    public string? OnlineUrl { get; set; }
    public int? MaxParticipants { get; set; }
    public bool IsMandatory { get; set; }
    public bool HasCertification { get; set; }
    public int? ValidityMonths { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<EmployeeTraining> EmployeeTrainings { get; set; } = new List<EmployeeTraining>();
}

public enum TrainingType
{
    Online = 0,
    InPerson = 1,
    Hybrid = 2,
    SelfPaced = 3,
    Webinar = 4,
    Workshop = 5
}
