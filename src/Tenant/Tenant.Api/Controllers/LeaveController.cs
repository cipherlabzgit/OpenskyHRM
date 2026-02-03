using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tenant.Domain.Entities;
using Tenant.Infrastructure.Data;

namespace Tenant.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly TenantDbContext _context;
    private readonly ILogger<LeaveController> _logger;

    public LeaveController(TenantDbContext context, ILogger<LeaveController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests(
        [FromQuery] Guid? employeeId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = _context.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.LeaveType)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(r => r.EmployeeId == employeeId.Value);
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LeaveRequestStatus>(status, true, out var statusEnum))
        {
            query = query.Where(r => r.Status == statusEnum);
        }

        var requests = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new
            {
                r.Id,
                r.EmployeeId,
                EmployeeName = r.Employee != null ? r.Employee.FullName : null,
                LeaveType = r.LeaveType != null ? r.LeaveType.Name : null,
                LeaveTypeId = r.LeaveTypeId,
                r.StartDate,
                r.EndDate,
                r.Reason,
                r.Status,
                StatusName = r.Status.ToString(),
                r.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(requests);
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateLeaveRequestDto request, CancellationToken cancellationToken)
    {
        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            Status = LeaveRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { leaveRequest.Id, message = "Leave request submitted successfully" });
    }

    [HttpPut("requests/{id}/approve")]
    public async Task<IActionResult> ApproveRequest(Guid id, [FromBody] ApproveRejectDto dto, CancellationToken cancellationToken)
    {
        var request = await _context.LeaveRequests.FindAsync(new object[] { id }, cancellationToken);
        if (request == null)
        {
            return NotFound(new { error = "Leave request not found" });
        }

        if (request.Status != LeaveRequestStatus.Pending)
        {
            return BadRequest(new { error = "Only pending requests can be approved" });
        }

        request.Status = LeaveRequestStatus.Approved;
        request.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Leave request approved" });
    }

    [HttpPut("requests/{id}/reject")]
    public async Task<IActionResult> RejectRequest(Guid id, [FromBody] ApproveRejectDto dto, CancellationToken cancellationToken)
    {
        var request = await _context.LeaveRequests.FindAsync(new object[] { id }, cancellationToken);
        if (request == null)
        {
            return NotFound(new { error = "Leave request not found" });
        }

        if (request.Status != LeaveRequestStatus.Pending)
        {
            return BadRequest(new { error = "Only pending requests can be rejected" });
        }

        request.Status = LeaveRequestStatus.Rejected;
        request.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Leave request rejected" });
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetLeaveTypes(CancellationToken cancellationToken)
    {
        var types = await _context.LeaveTypes
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.Name, t.Code, t.DefaultDays, t.IsPaid })
            .ToListAsync(cancellationToken);

        return Ok(types);
    }

    [HttpGet("balances/{employeeId}")]
    public async Task<IActionResult> GetBalances(Guid employeeId, CancellationToken cancellationToken)
    {
        var year = DateTime.Now.Year;
        var balances = await _context.LeaveBalances
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employeeId && b.Year == year)
            .Select(b => new
            {
                b.Id,
                LeaveType = b.LeaveType != null ? b.LeaveType.Name : null,
                b.Entitled,
                b.Used,
                b.Balance,
                b.Year
            })
            .ToListAsync(cancellationToken);

        return Ok(balances);
    }
}

public class CreateLeaveRequestDto
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
}

public class ApproveRejectDto
{
    public string? Comments { get; set; }
}
