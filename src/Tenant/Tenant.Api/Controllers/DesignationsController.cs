using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DesignationsController : ControllerBase
{
    private readonly TenantDbContext _context;

    public DesignationsController(TenantDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var designations = await _context.Designations
            .Select(d => new { d.Id, d.Name, d.Code, d.Level, d.CreatedAtUtc })
            .ToListAsync();
        return Ok(designations);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var designation = await _context.Designations.FindAsync(id);
        if (designation == null) return NotFound();
        return Ok(designation);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDesignationRequest request)
    {
        var designation = new Designation
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            Level = request.Level,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Designations.Add(designation);
        await _context.SaveChangesAsync();
        return Ok(designation);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateDesignationRequest request)
    {
        var designation = await _context.Designations.FindAsync(id);
        if (designation == null) return NotFound();
        
        designation.Name = request.Name;
        designation.Code = request.Code;
        designation.Level = request.Level;
        designation.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(designation);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var designation = await _context.Designations.FindAsync(id);
        if (designation == null) return NotFound();
        _context.Designations.Remove(designation);
        await _context.SaveChangesAsync();
        return Ok();
    }
}

public class CreateDesignationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int? Level { get; set; }
}
