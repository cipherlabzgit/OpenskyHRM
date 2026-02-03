namespace Tenant.Domain.Entities;

public class Announcement
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AnnouncementType Type { get; set; } = AnnouncementType.General;
    public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Normal;
    public DateTime PublishDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? TargetAudience { get; set; } // All, Department IDs, Branch IDs
    public bool RequiresAcknowledgment { get; set; }
    public bool IsPinned { get; set; }
    public Guid? CreatedById { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum AnnouncementType
{
    General = 0,
    Policy = 1,
    Event = 2,
    Urgent = 3,
    Celebration = 4
}

public enum AnnouncementPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}
