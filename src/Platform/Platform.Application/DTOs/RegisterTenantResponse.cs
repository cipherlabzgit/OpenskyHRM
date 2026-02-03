namespace Platform.Application.DTOs;

public class RegisterTenantResponse
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string TenantUrl { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
