using Microsoft.AspNetCore.Mvc;
using Platform.Application.DTOs;
using Platform.Application.Interfaces;
using Platform.Application.Services;

namespace Platform.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TenantController : ControllerBase
{
    private readonly TenantProvisioningService _provisioningService;
    private readonly ITenantsRepository _tenantsRepository;
    private readonly ILogger<TenantController> _logger;

    public TenantController(
        TenantProvisioningService provisioningService,
        ITenantsRepository tenantsRepository,
        ILogger<TenantController> logger)
    {
        _provisioningService = provisioningService;
        _tenantsRepository = tenantsRepository;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterTenantResponse>> Register(
        [FromBody] RegisterTenantRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Registering new tenant: {CompanyName}", request.CompanyName);
            var result = await _provisioningService.CreateTenantAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Tenant registration validation failed");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register tenant: {CompanyName}", request.CompanyName);
            return StatusCode(500, new { error = "Failed to create tenant. Please try again." });
        }
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> LookupByEmail([FromQuery] string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { error = "Email is required" });
        }

        _logger.LogInformation("Looking up tenant for email: {Email}", email);
        
        var tenant = await _tenantsRepository.GetByAdminEmailAsync(email.ToLowerInvariant(), cancellationToken);
        
        if (tenant == null)
        {
            return NotFound(new { error = "No tenant found for this email address" });
        }

        return Ok(new { 
            tenantCode = tenant.TenantCode,
            companyName = tenant.CompanyName
        });
    }
}
