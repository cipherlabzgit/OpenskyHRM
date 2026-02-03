using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Tenant.Application.Interfaces;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;

namespace Tenant.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly TenantDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        TenantDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(User User, string AccessToken, string RefreshToken, DateTime ExpiresAt)> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        _logger.LogInformation("Login attempt for email: {Email}", normalizedEmail);

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User not found: {Email}", normalizedEmail);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("User account is inactive: {Email}", normalizedEmail);
            throw new UnauthorizedAccessException("Account is inactive");
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid password for user: {Email}", normalizedEmail);
            user.AccessFailedCount++;
            await _context.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Reset failed count on successful login
        user.AccessFailedCount = 0;
        user.LockoutEndUtc = null;
        await _context.SaveChangesAsync(cancellationToken);

        var (accessToken, expiresAt) = GenerateJwtToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, cancellationToken);

        _logger.LogInformation("Login successful for user: {UserId}", user.Id);

        return (user, accessToken, refreshToken, expiresAt);
    }

    public async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.RevokedAtUtc == null, cancellationToken);

        if (token == null || token.ExpiresAtUtc < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        // Revoke old token
        token.RevokedAtUtc = DateTime.UtcNow;
        token.RevokedReason = "Replaced";

        var (newAccessToken, expiresAt) = GenerateJwtToken(token.User);
        var newRefreshToken = await GenerateRefreshTokenAsync(token.UserId, cancellationToken);

        return (newAccessToken, newRefreshToken, expiresAt);
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var salt = _configuration["Jwt:Salt"] ?? "default-salt";
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string? hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        return HashPassword(password) == hash;
    }

    private (string Token, DateTime ExpiresAt) GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? "your-256-bit-secret-key-here-must-be-at-least-32-characters-long"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("userId", user.Id.ToString())
        };

        // Add roles
        if (user.UserRoles != null)
        {
            foreach (var userRole in user.UserRoles.Where(ur => ur.Role != null))
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole.Role!.Name));
            }
        }

        var expiresAt = DateTime.UtcNow.AddHours(24);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "HrSaaS",
            audience: _configuration["Jwt:Audience"] ?? "HrSaaS",
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private async Task<string> GenerateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task<User> CreateUserAsync(
        string email,
        string password,
        string fullName,
        Guid? roleId = null,
        CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = HashPassword(password),
            FullName = fullName,
            IsActive = true,
            EmailConfirmed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Users.Add(user);

        if (roleId.HasValue)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId.Value,
                AssignedAtUtc = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }
}
