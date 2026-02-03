using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class HolidaysController : ControllerBase
{
    private readonly TenantDbContext _context;

    public HolidaysController(TenantDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? year)
    {
        var query = _context.Holidays.Where(h => h.IsActive).AsQueryable();
        if (year.HasValue)
            query = query.Where(h => h.Date.Year == year.Value);

        var holidays = await query
            .OrderBy(h => h.Date)
            .Select(h => new { h.Id, h.Name, h.Date, h.Type, h.IsRecurring, h.IsOptional, h.Description })
            .ToListAsync();
        return Ok(holidays);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var holiday = await _context.Holidays.FindAsync(id);
        if (holiday == null) return NotFound();
        return Ok(holiday);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHolidayRequest request)
    {
        var holiday = new Holiday
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Date = request.Date,
            Type = request.Type,
            IsRecurring = request.IsRecurring,
            Description = request.Description,
            ApplicableBranches = request.ApplicableBranches,
            ApplicableDepartments = request.ApplicableDepartments,
            IsOptional = request.IsOptional,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Holidays.Add(holiday);
        await _context.SaveChangesAsync();
        return Ok(holiday);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateHolidayRequest request)
    {
        var holiday = await _context.Holidays.FindAsync(id);
        if (holiday == null) return NotFound();
        
        holiday.Name = request.Name;
        holiday.Date = request.Date;
        holiday.Type = request.Type;
        holiday.IsRecurring = request.IsRecurring;
        holiday.Description = request.Description;
        holiday.ApplicableBranches = request.ApplicableBranches;
        holiday.ApplicableDepartments = request.ApplicableDepartments;
        holiday.IsOptional = request.IsOptional;
        holiday.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(holiday);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var holiday = await _context.Holidays.FindAsync(id);
        if (holiday == null) return NotFound();
        holiday.IsActive = false;
        holiday.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok();
    }
}

public class CreateHolidayRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public HolidayType Type { get; set; }
    public bool IsRecurring { get; set; }
    public string? Description { get; set; }
    public string? ApplicableBranches { get; set; }
    public string? ApplicableDepartments { get; set; }
    public bool IsOptional { get; set; }
}
