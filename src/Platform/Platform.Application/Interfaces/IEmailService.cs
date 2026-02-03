namespace Platform.Application.Interfaces;

public interface IEmailService
{
    Task SendTenantRegistrationEmailAsync(string email, string tenantCode, string companyName, string tenantUrl, CancellationToken cancellationToken = default);
}
