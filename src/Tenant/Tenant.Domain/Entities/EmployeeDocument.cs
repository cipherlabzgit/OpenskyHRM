namespace Tenant.Domain.Entities;

public class EmployeeDocument
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string DocumentType { get; set; } = string.Empty; // Resume, Contract, ID, Certificate, etc.
    public string FileName { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public string? MimeType { get; set; }
    public long? FileSize { get; set; }
    public string? Description { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsVerified { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
