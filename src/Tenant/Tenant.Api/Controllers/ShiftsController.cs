using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ShiftsController : ControllerBase
{
    private readonly TenantDbContext _context;

    public ShiftsController(TenantDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var shifts = await _context.ShiftTemplates
            .Where(s => s.IsActive)
            .Select(s => new { s.Id, s.Name, s.Code, s.StartTime, s.EndTime, s.WorkingHours, s.IsNightShift, s.Color })
            .ToListAsync();
        return Ok(shifts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var shift = await _context.ShiftTemplates.FindAsync(id);
        if (shift == null) return NotFound();
        return Ok(shift);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShiftRequest request)
    {
        var shift = new ShiftTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            WorkingHours = request.WorkingHours,
            IsNightShift = request.IsNightShift,
            IsFlexible = request.IsFlexible,
            GracePeriodMinutes = request.GracePeriodMinutes,
            Color = request.Color,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ShiftTemplates.Add(shift);
        await _context.SaveChangesAsync();
        return Ok(shift);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateShiftRequest request)
    {
        var shift = await _context.ShiftTemplates.FindAsync(id);
        if (shift == null) return NotFound();
        
        shift.Name = request.Name;
        shift.Code = request.Code;
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.WorkingHours = request.WorkingHours;
        shift.IsNightShift = request.IsNightShift;
        shift.IsFlexible = request.IsFlexible;
        shift.GracePeriodMinutes = request.GracePeriodMinutes;
        shift.Color = request.Color;
        shift.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(shift);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var shift = await _context.ShiftTemplates.FindAsync(id);
        if (shift == null) return NotFound();
        shift.IsActive = false;
        shift.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok();
    }
}

public class CreateShiftRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal WorkingHours { get; set; }
    public bool IsNightShift { get; set; }
    public bool IsFlexible { get; set; }
    public int? GracePeriodMinutes { get; set; }
    public string? Color { get; set; }
}
