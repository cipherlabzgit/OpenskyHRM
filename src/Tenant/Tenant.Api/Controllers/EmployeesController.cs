using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly TenantDbContext _context;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(TenantDbContext context, ILogger<EmployeesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] string? search,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.FullName.Contains(search) ||
                e.EmployeeCode.Contains(search) ||
                (e.Email != null && e.Email.Contains(search)));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.EmploymentStatus == status);
        }

        var total = await query.CountAsync(cancellationToken);
        
        var employees = await query
            .OrderBy(e => e.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.Email,
                e.Phone,
                Department = e.Department != null ? e.Department.Name : null,
                DepartmentId = e.DepartmentId,
                Designation = e.Designation != null ? e.Designation.Name : null,
                DesignationId = e.DesignationId,
                e.JoinedDate,
                e.EmploymentStatus,
                e.ProfilePhotoUrl
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            data = employees,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.Branch)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee == null)
            return NotFound(new { error = "Employee not found" });

        return Ok(new
        {
            employee.Id,
            employee.EmployeeCode,
            employee.FirstName,
            employee.MiddleName,
            employee.LastName,
            employee.FullName,
            employee.Email,
            employee.PersonalEmail,
            employee.Phone,
            employee.MobilePhone,
            employee.DateOfBirth,
            employee.Gender,
            employee.MaritalStatus,
            employee.Address,
            employee.City,
            employee.State,
            employee.Country,
            employee.PostalCode,
            Department = employee.Department?.Name,
            DepartmentId = employee.DepartmentId,
            Designation = employee.Designation?.Name,
            DesignationId = employee.DesignationId,
            Branch = employee.Branch?.Name,
            BranchId = employee.BranchId,
            employee.JoinedDate,
            employee.EmploymentType,
            employee.EmploymentStatus,
            employee.ProfilePhotoUrl,
            employee.Bio
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto request, CancellationToken cancellationToken)
    {
        var employeeCode = request.EmployeeCode ?? $"EMP{DateTime.UtcNow:yyyyMMddHHmmss}";

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = employeeCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            FullName = $"{request.FirstName} {request.LastName}".Trim(),
            Email = request.Email,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            DepartmentId = request.DepartmentId,
            DesignationId = request.DesignationId,
            BranchId = request.BranchId,
            JoinedDate = request.JoinDate,
            EmploymentType = request.EmploymentType ?? "FullTime",
            EmploymentStatus = "Active",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { employee.Id, employee.EmployeeCode, message = "Employee created successfully" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeDto request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
        if (employee == null)
            return NotFound(new { error = "Employee not found" });

        employee.FirstName = request.FirstName ?? employee.FirstName;
        employee.LastName = request.LastName ?? employee.LastName;
        employee.FullName = $"{employee.FirstName} {employee.LastName}".Trim();
        employee.Email = request.Email ?? employee.Email;
        employee.Phone = request.Phone ?? employee.Phone;
        employee.DepartmentId = request.DepartmentId ?? employee.DepartmentId;
        employee.DesignationId = request.DesignationId ?? employee.DesignationId;
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Employee updated successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
        if (employee == null)
            return NotFound(new { error = "Employee not found" });

        employee.EmploymentStatus = "Terminated";
        employee.TerminationDate = DateTime.Today;
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Employee terminated successfully" });
    }
}

public class CreateEmployeeDto
{
    public string? EmployeeCode { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime? JoinDate { get; set; }
    public string? EmploymentType { get; set; }
}

public class UpdateEmployeeDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? DesignationId { get; set; }
}
