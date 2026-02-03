using Microsoft.AspNetCore.Mvc;
using Tenant.Application.Interfaces;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Login attempt for tenant: {TenantCode}, email: {Email}", 
                request.TenantCode, request.Email);

            var (user, accessToken, refreshToken, expiresAt) = await _authService.LoginAsync(
                request.Email,
                request.Password,
                cancellationToken);

            return Ok(new LoginResponse
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                FullName = user.FullName,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                Roles = user.UserRoles?.Select(ur => ur.Role?.Name ?? "").Where(r => !string.IsNullOrEmpty(r)).ToList() ?? new List<string>()
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login failed: {Message}", ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for email: {Email}", request.Email);
            return StatusCode(500, new { error = "An error occurred during login. Please try again." });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var (accessToken, refreshToken, expiresAt) = await _authService.RefreshTokenAsync(
                request.RefreshToken,
                cancellationToken);

            return Ok(new
            {
                accessToken,
                refreshToken,
                expiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return Unauthorized(new { error = "Invalid or expired refresh token" });
        }
    }
}

public class LoginRequest
{
    public string TenantCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
