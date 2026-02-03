namespace Tenant.Domain.Entities;

public class EmailLog
{
    public Guid Id { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string EmailType { get; set; } = string.Empty; // ApplicationReceived, InterviewScheduled, OfferExtended, etc.
    public Guid? RelatedEntityId { get; set; } // ApplicationId, InterviewId, OfferId, etc.
    public string? RelatedEntityType { get; set; }
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public enum EmailStatus
{
    Pending = 0,
    Sent = 1,
    Delivered = 2,
    Opened = 3,
    Failed = 4,
    Bounced = 5
}
