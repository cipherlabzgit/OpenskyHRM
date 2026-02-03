namespace Tenant.Domain.Entities;

public class CandidateDocument
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Candidate Candidate { get; set; } = null!;
    public string DocumentType { get; set; } = string.Empty; // Resume, CoverLetter, Certificate, Portfolio, etc.
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? MimeType { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public Guid? UploadedById { get; set; }
    public User? UploadedBy { get; set; }
}
