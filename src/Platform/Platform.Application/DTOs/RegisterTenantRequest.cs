namespace Platform.Application.DTOs;

public class RegisterTenantRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? TimeZone { get; set; }
    public string? Currency { get; set; }
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string? AdminFullName { get; set; }
}
