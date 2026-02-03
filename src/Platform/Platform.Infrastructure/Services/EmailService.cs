using Microsoft.Extensions.Logging;
using Platform.Application.Interfaces;

namespace Platform.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendTenantRegistrationEmailAsync(string email, string tenantCode, string companyName, string tenantUrl, CancellationToken cancellationToken = default)
    {
        // In production, integrate with an email service like SendGrid, AWS SES, etc.
        // For now, log the email details
        _logger.LogInformation(
            "TENANT REGISTRATION EMAIL\n" +
            "=========================\n" +
            "To: {Email}\n" +
            "Company: {CompanyName}\n" +
            "Tenant Code: {TenantCode}\n" +
            "URL: {TenantUrl}\n" +
            "=========================",
            email, companyName, tenantCode, tenantUrl);

        // Simulate async operation
        await Task.CompletedTask;
    }
}
