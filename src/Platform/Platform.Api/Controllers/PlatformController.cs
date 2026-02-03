using Microsoft.AspNetCore.Mvc;
using Platform.Application.Interfaces;

namespace Platform.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PlatformController : ControllerBase
{
    private readonly ITenantsRepository _tenantsRepository;
    private readonly ILogger<PlatformController> _logger;

    public PlatformController(
        ITenantsRepository tenantsRepository,
        ILogger<PlatformController> logger)
    {
        _tenantsRepository = tenantsRepository;
        _logger = logger;
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants(CancellationToken cancellationToken)
    {
        var tenants = await _tenantsRepository.GetAllAsync(cancellationToken);
        return Ok(tenants.Select(t => new
        {
            t.TenantId,
            t.TenantCode,
            t.CompanyName,
            t.Status,
            t.CreatedAtUtc
        }));
    }

    [HttpGet("tenants/{tenantCode}")]
    public async Task<IActionResult> GetTenant(string tenantCode, CancellationToken cancellationToken)
    {
        var tenant = await _tenantsRepository.GetByTenantCodeAsync(tenantCode, cancellationToken);
        if (tenant == null)
            return NotFound(new { error = "Tenant not found" });

        return Ok(new
        {
            tenant.TenantId,
            tenant.TenantCode,
            tenant.CompanyName,
            tenant.LegalName,
            tenant.Country,
            tenant.TimeZone,
            tenant.Currency,
            tenant.Status,
            tenant.CreatedAtUtc
        });
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
