using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly TenantDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(TenantDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        
        var totalEmployees = await _context.Employees
            .CountAsync(e => e.EmploymentStatus == "Active", cancellationToken);

        var pendingLeaves = await _context.LeaveRequests
            .CountAsync(l => l.Status == LeaveRequestStatus.Pending, cancellationToken);

        var presentToday = await _context.AttendanceRecords
            .CountAsync(a => a.Date == today && a.Status == AttendanceStatus.Present, cancellationToken);

        var departments = await _context.Departments.CountAsync(cancellationToken);

        return Ok(new
        {
            totalEmployees,
            pendingLeaves,
            presentToday,
            departments
        });
    }

    [HttpGet("attendance-summary")]
    public async Task<IActionResult> GetAttendanceSummary([FromQuery] int days = 7, CancellationToken cancellationToken = default)
    {
        var startDate = DateTime.Today.AddDays(-days);
        
        var summary = await _context.AttendanceRecords
            .Where(a => a.Date >= startDate)
            .GroupBy(a => a.Date)
            .Select(g => new
            {
                Date = g.Key,
                Present = g.Count(x => x.Status == AttendanceStatus.Present),
                Absent = g.Count(x => x.Status == AttendanceStatus.Absent),
                Late = g.Count(x => x.Status == AttendanceStatus.Late)
            })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        return Ok(summary);
    }

    [HttpGet("leave-summary")]
    public async Task<IActionResult> GetLeaveSummary(CancellationToken cancellationToken)
    {
        var summary = await _context.LeaveRequests
            .GroupBy(l => l.Status)
            .Select(g => new
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        return Ok(summary);
    }

    [HttpGet("recent-activities")]
    public async Task<IActionResult> GetRecentActivities(CancellationToken cancellationToken)
    {
        var recentLeaves = await _context.LeaveRequests
            .Include(l => l.Employee)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(5)
            .Select(l => new
            {
                Type = "Leave Request",
                Description = $"{l.Employee!.FullName} requested leave",
                Date = l.CreatedAtUtc,
                Status = l.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        return Ok(recentLeaves);
    }
}
