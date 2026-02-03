using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;
using Tenant.Infrastructure.Services;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly TenantDbContext _context;
    private readonly ILogger<UsersController> _logger;
    private readonly IConfiguration _configuration;

    public UsersController(
        TenantDbContext context,
        ILogger<UsersController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                u.IsActive,
                u.EmailConfirmed,
                Roles = u.UserRoles.Select(ur => new { ur.Role.Id, ur.Role.Name }),
                u.CreatedAtUtc
            })
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound(new { error = "User not found" });

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FullName,
            user.IsActive,
            user.EmailConfirmed,
            Roles = user.UserRoles.Select(ur => new { ur.Role.Id, ur.Role.Name }),
            user.CreatedAtUtc
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
        
        if (existingUser != null)
        {
            return Conflict(new { error = $"User with email '{request.Email}' already exists" });
        }

        // Hash password
        var passwordHash = HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLower(),
            PasswordHash = passwordHash,
            FullName = request.FullName,
            IsActive = true,
            EmailConfirmed = false,
            AccessFailedCount = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Users.Add(user);

        // Assign roles if provided
        if (request.RoleIds != null && request.RoleIds.Any())
        {
            foreach (var roleId in request.RoleIds)
            {
                var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId);
                if (roleExists)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleId,
                        AssignedAtUtc = DateTime.UtcNow
                    };
                    _context.UserRoles.Add(userRole);
                }
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created user: {Email} with roles: {RoleIds}", user.Email, string.Join(", ", request.RoleIds ?? Array.Empty<Guid>()));

        return Ok(new { user.Id, user.Email, user.FullName, message = "User created successfully" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound(new { error = "User not found" });

        // Update basic info
        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName;

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        // Update password if provided
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = HashPassword(request.Password);
        }

        // Update roles if provided
        if (request.RoleIds != null)
        {
            // Remove existing roles
            var existingRoles = user.UserRoles.ToList();
            _context.UserRoles.RemoveRange(existingRoles);

            // Add new roles
            foreach (var roleId in request.RoleIds)
            {
                var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId);
                if (roleExists)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleId,
                        AssignedAtUtc = DateTime.UtcNow
                    };
                    _context.UserRoles.Add(userRole);
                }
            }
        }

        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "User updated successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { error = "User not found" });

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User deleted successfully" });
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _context.Roles
            .Select(r => new { r.Id, r.Name, r.Description })
            .ToListAsync();
        return Ok(roles);
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var salt = _configuration["Jwt:Salt"] ?? "default-salt";
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
        return Convert.ToBase64String(hashedBytes);
    }
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<Guid>? RoleIds { get; set; }
}

public class UpdateUserRequest
{
    public string? FullName { get; set; }
    public string? Password { get; set; }
    public bool? IsActive { get; set; }
    public List<Guid>? RoleIds { get; set; }
}
