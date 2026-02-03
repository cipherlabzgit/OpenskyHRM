using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly TenantDbContext _context;

    public DepartmentsController(TenantDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var departments = await _context.Departments
            .Select(d => new { d.Id, d.Name, d.Code, d.ParentId, d.CreatedAtUtc })
            .ToListAsync();
        return Ok(departments);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null) return NotFound();
        return Ok(department);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            ParentId = request.ParentId,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();
        return Ok(department);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateDepartmentRequest request)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null) return NotFound();
        
        department.Name = request.Name;
        department.Code = request.Code;
        department.ParentId = request.ParentId;
        department.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(department);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null) return NotFound();
        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();
        return Ok();
    }
}

public class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public Guid? ParentId { get; set; }
}
