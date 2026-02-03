namespace Tenant.Domain.Entities;

public class ApplicationActivity
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    public ActivityType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? PerformedById { get; set; }
    public User? PerformedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public enum ActivityType
{
    Applied = 0,
    StatusChanged = 1,
    StageChanged = 2,
    InterviewScheduled = 3,
    InterviewCompleted = 4,
    AssessmentAssigned = 5,
    AssessmentCompleted = 6,
    OfferExtended = 7,
    OfferAccepted = 8,
    OfferRejected = 9,
    NoteAdded = 10,
    DocumentUploaded = 11,
    EmailSent = 12,
    TagAdded = 13,
    RatingChanged = 14
}
