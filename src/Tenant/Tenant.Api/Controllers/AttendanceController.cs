using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly TenantDbContext _context;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(TenantDbContext context, ILogger<AttendanceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("records")]
    public async Task<IActionResult> GetRecords([FromQuery] DateTime? date, [FromQuery] Guid? employeeId, CancellationToken cancellationToken)
    {
        var query = _context.AttendanceRecords
            .Include(r => r.Employee)
            .AsQueryable();

        if (date.HasValue)
        {
            query = query.Where(r => r.Date.Date == date.Value.Date);
        }
        else
        {
            query = query.Where(r => r.Date.Date == DateTime.Today);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(r => r.EmployeeId == employeeId.Value);
        }

        var records = await query
            .OrderByDescending(r => r.Date)
            .Select(r => new
            {
                r.Id,
                r.EmployeeId,
                EmployeeName = r.Employee != null ? r.Employee.FullName : null,
                EmployeeCode = r.Employee != null ? r.Employee.EmployeeCode : null,
                r.Date,
                r.CheckInTime,
                r.CheckOutTime,
                r.Status,
                StatusName = r.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        return Ok(records);
    }

    [HttpPost("clock-in")]
    public async Task<IActionResult> ClockIn([FromBody] ClockInRequest request, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var existing = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == request.EmployeeId && r.Date == today, cancellationToken);

        if (existing != null && existing.CheckInTime.HasValue)
        {
            return BadRequest(new { error = "Already clocked in today" });
        }

        var now = DateTime.Now.TimeOfDay;

        if (existing == null)
        {
            existing = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = request.EmployeeId,
                Date = today,
                CheckInTime = now,
                Status = AttendanceStatus.Present,
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.AttendanceRecords.Add(existing);
        }
        else
        {
            existing.CheckInTime = now;
            existing.Status = AttendanceStatus.Present;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Clocked in successfully", time = now });
    }

    [HttpPost("clock-out")]
    public async Task<IActionResult> ClockOut([FromBody] ClockOutRequest request, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == request.EmployeeId && r.Date == today, cancellationToken);

        if (record == null || !record.CheckInTime.HasValue)
        {
            return BadRequest(new { error = "Must clock in first" });
        }

        if (record.CheckOutTime.HasValue)
        {
            return BadRequest(new { error = "Already clocked out today" });
        }

        var now = DateTime.Now.TimeOfDay;
        record.CheckOutTime = now;
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Clocked out successfully", time = now });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken cancellationToken)
    {
        var start = startDate ?? DateTime.Today.AddDays(-30);
        var end = endDate ?? DateTime.Today;

        var records = await _context.AttendanceRecords
            .Where(r => r.Date >= start && r.Date <= end)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            startDate = start,
            endDate = end,
            summary = records
        });
    }
}

public class ClockInRequest
{
    public Guid EmployeeId { get; set; }
    public string? Notes { get; set; }
}

public class ClockOutRequest
{
    public Guid EmployeeId { get; set; }
    public string? Notes { get; set; }
}
